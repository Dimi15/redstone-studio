using System.Collections;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace redstone_stone.Domain.Entities;

public class Block : Entity
{
    private bool _hard { get; set; }
    
    public Block() : base(x, y)
    {
        _hard = false;
    }

    public bool IsHard()
    {

        if (Nearby.North() == repeater) _hard = repeater.GetDirection() == sud;
        else if (Nearby.East() == repeater) _hard = repeater.GetDirection() == West;
        else if (Nearby.Sud() == repeater) _hard = repeater.GetDirection() == north;
        else if (Nearby.West() == repeater) _hard = repeater.GetDirection() == East;

        if (_hard) return _hard;

        if (Nearby.North().IsPowering() || Nearby.East().IsPowering() || Nearby.Sud().IsPowering() || Nearby.West().IsPowering()) _hard = false;

        return _hard;
    }

    public override IsPowered()
    {
        
    }
}