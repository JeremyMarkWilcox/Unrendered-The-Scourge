using Godot;

namespace Unrendered.Dream;

/// <summary>
/// Makes a light gutter. Drop it as a child of any <see cref="Light3D"/> and that light stops being
/// static; nothing else needs to know.
/// </summary>
/// <remarks>
/// <para>
/// <b>It moves energy, never range, and that is the load-bearing decision.</b> The Resolve degrade is
/// specified as a *shrinking light radius* — one number, no shader parameter, the dark closing in. If
/// flicker also touched the radius the player could not tell "my nerve is going" from "my lamp is
/// guttering", and the diegetic readout would be dead. Same class of collision as the three systems
/// that all wanted to encode distance as value, and the same fix: one owner per cue. Radius belongs to
/// Resolve; brightness is free.
/// </para>
/// <para>
/// The two still separate in *time* even though both change the lit footprint at low grey counts, and
/// that is the second line of defence: the degrade is slow and monotonic, flicker is fast and
/// non-monotonic. If the degrade is ever animated fast, revisit this.
/// </para>
/// <para>
/// <b>A free consequence worth knowing, because it argues for keeping creatures unshaded.</b>
/// Creature materials are unshaded, so a creature is completely immune to this. When your lamp
/// gutters, the room flutters and the thing standing in it does not — it stays exactly as visible
/// while your own light fails. That is a better effect than flickering it too, and it came out of a
/// decision made for a different reason.
/// </para>
/// <para>
/// Noise, not a sine: a periodic flicker reads as mechanical. And the noise is seeded per light so
/// three lights never pulse in lockstep — in lockstep it reads as one global effect on the screen
/// rather than as separate failing lights in a room, which is the whole point.
/// </para>
/// <para>
/// <b>Character is authored per light; magnitude is scrubbed globally.</b> <see cref="Amount"/> and
/// <see cref="Speed"/> live on the node, so a player lamp can be subtle and a wall sconce can be
/// desperate; <see cref="Scale"/> and <see cref="SpeedScale"/> are driven by the tuner so the whole
/// room can be dialled at once without flattening the differences between lights.
/// </para>
/// </remarks>
public partial class DreamLightFlicker : Node
{
    /// <summary>How deep this particular light dips, as a fraction of its authored energy.</summary>
    [Export(PropertyHint.Range, "0,1")] public float Amount { get; set; } = 0.3f;

    /// <summary>Re-rolls per second for this light.</summary>
    [Export(PropertyHint.Range, "0,30")] public float Speed { get; set; } = 9.0f;

    /// <summary>Leave at 0 to derive a stable one from the node name.</summary>
    [Export] public int Seed { get; set; }

    /// <summary>Global multiplier on <see cref="Amount"/>. 0 turns flicker off entirely.</summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>Global multiplier on <see cref="Speed"/>.</summary>
    public float SpeedScale { get; set; } = 1.0f;

    private Light3D _light;
    private float _baseEnergy;
    private float _time;

    public override void _Ready()
    {
        _light = GetParent<Light3D>();
        if (_light == null)
        {
            GD.PushError($"[flicker] {Name} is not a child of a Light3D; nothing to gutter.");
            SetProcess(false);
            return;
        }

        // Captured once. Read live it would compound with itself and walk the light to zero.
        _baseEnergy = _light.LightEnergy;

        if (Seed == 0)
        {
            Seed = StableSeed(Name);
        }
    }

    public override void _Process(double delta)
    {
        _time += (float)delta * Speed * SpeedScale;

        // Never brighter than authored, only dimmer: a guttering lamp dips, it does not flare. Also
        // keeps a bright surface from being pushed anywhere near the top of the ramp by a flicker.
        var amount = Mathf.Clamp(Amount * Scale, 0.0f, 1.0f);
        _light.LightEnergy = _baseEnergy * Mathf.Lerp(1.0f - amount, 1.0f, Noise(_time));
    }

    private float Noise(float t)
    {
        var whole = Mathf.FloorToInt(t);
        var fraction = t - whole;
        return Mathf.Lerp(Hash(whole), Hash(whole + 1), fraction * fraction * (3.0f - (2.0f * fraction)));
    }

    private float Hash(int step)
    {
        unchecked
        {
            var x = (uint)(step + Seed) * 2654435761u;
            x ^= x >> 15;
            x *= 2246822519u;
            x ^= x >> 13;
            return (x & 0xFFFFFFu) / (float)0xFFFFFF;
        }
    }

    /// <summary>
    /// A deterministic hash of the node name. Deliberately not <c>string.GetHashCode</c>, which is
    /// randomised per process in .NET — the flicker would differ between runs and a screenshot could
    /// never be reproduced.
    /// </summary>
    private static int StableSeed(string name)
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in name)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return (int)(hash & 0x7FFFFFFFu);
        }
    }
}
