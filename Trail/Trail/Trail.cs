using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Trail;

public class TrailConfig : BasePluginConfig
{
    [JsonPropertyName("Colors")]
    public Dictionary<string, string> Colors { get; set; } = new()
    {
        { "blue", "0,0,255" },
        { "red", "255,0,0" },
        { "green", "0,255,0" },
        { "yellow", "255,255,0" },
        { "purple", "128,0,128" },
        { "cyan", "0,255,255" }
    };

    [JsonPropertyName("BeamWidth")]
    public float BeamWidth { get; set; } = 2.0f;

    [JsonPropertyName("BeamLife")]
    public float BeamLife { get; set; } = 0.5f;
}

public class BeamTrail : BasePlugin, IPluginConfig<TrailConfig>
{
    public override string ModuleName => "Beam Trail";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "QuryWesT";

    public TrailConfig Config { get; set; } = new();

    private Dictionary<int, Color> _playerTrailColor = new();
    private Dictionary<int, Vector> _lastPositions = new();

    public void OnConfigParsed(TrailConfig config)
    {
        this.Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (IsValidPlayer(player) && _playerTrailColor.ContainsKey(player.Slot))
                {
                    var playerPawn = player.PlayerPawn.Value;
                    if (playerPawn == null) continue;

                    Vector currentPos = playerPawn.AbsOrigin!;
                    if (_lastPositions.TryGetValue(player.Slot, out var lastPos))
                    {
                        if ((currentPos - lastPos).Length() > 15.0f)
                        {
                            CreateBeam(
                                new Vector(lastPos.X, lastPos.Y, lastPos.Z + 5.0f),
                                new Vector(currentPos.X, currentPos.Y, currentPos.Z + 5.0f),
                                _playerTrailColor[player.Slot]
                            );
                            _lastPositions[player.Slot] = new Vector(currentPos.X, currentPos.Y, currentPos.Z);
                        }
                    }
                    else { _lastPositions[player.Slot] = new Vector(currentPos.X, currentPos.Y, currentPos.Z); }
                }
            }
        });
    }

    private void CreateBeam(Vector start, Vector end, Color color)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null) return;

        beam.Render = color;
        beam.Width = Config.BeamWidth;

        beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;

        beam.DispatchSpawn();

        AddTimer(Config.BeamLife, () => {
            if (beam != null && beam.IsValid) beam.Remove();
        });
    }

    [ConsoleCommand("css_trail", "Trail ayarlar.")]
    public void OnTrailCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.IsValid) return;

        if (info.ArgCount < 2)
        {
            string availableColors = string.Join(", ", Config.Colors.Keys);
            player.PrintToChat($" \x04[Trail]\x01 Kullanım: !trail <renk|off>");
            player.PrintToChat($" \x04[Renkler]\x01 {availableColors}");
            return;
        }

        string arg = info.GetArg(1).ToLower();

        if (arg == "off")
        {
            _playerTrailColor.Remove(player.Slot);
            player.PrintToChat(" \x04[Trail]\x01 Trail kapatıldı.");
            return;
        }

        if (Config.Colors.TryGetValue(arg, out string? rgbString))
        {
            var parts = rgbString.Split(',');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int r) &&
                int.TryParse(parts[1], out int g) &&
                int.TryParse(parts[2], out int b))
            {
                _playerTrailColor[player.Slot] = Color.FromArgb(r, g, b);
                player.PrintToChat($" \x04[Trail]\x01 Renk ayarlandı: \x06{arg}");
            }
        }
        else
        {
            player.PrintToChat($" \x02[Trail]\x01 '{arg}' geçerli bir renk değil.");
        }
    }

    private bool IsValidPlayer(CCSPlayerController? p) =>
        p != null && p.IsValid && p.PawnIsAlive && p.PlayerPawn.Value != null;
}