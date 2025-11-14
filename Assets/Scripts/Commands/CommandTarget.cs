using UnityEngine;

public enum CommandTargetType { None, Tile, Unit, Position }

public struct CommandTarget
{
    public CommandTargetType type;
    public int q;
    public int r;
    public UnitAgent unit;
    public Vector3 worldPos;

    public static CommandTarget ForTile(int q, int r)
    {
        return new CommandTarget { type = CommandTargetType.Tile, q = q, r = r, unit = null };
    }

    public static CommandTarget ForUnit(UnitAgent u)
    {
        return new CommandTarget { type = CommandTargetType.Unit, unit = u };
    }

    public static CommandTarget ForPosition(Vector3 pos)
    {
        return new CommandTarget { type = CommandTargetType.Position, worldPos = pos };
    }
}
