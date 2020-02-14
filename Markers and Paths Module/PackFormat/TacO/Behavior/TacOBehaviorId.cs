namespace Markers_and_Paths_Module.PackFormat.TacO.Behavior {
    public enum TacOBehaviorId : int {
        AlwaysVisible               = 0,
        ReappearOnMapChange         = 1,
        ReappearOnDailyReset        = 2,
        OnlyVisibleBeforeActivation = 3,
        ReappearAfterTimer          = 4,
        ReappearOnMapReset          = 5,
        OncePerInstance             = 6,
        DailyPerChar                = 7,

        OncePerInstancePerChar = 8,
        WvWObjective           = 9
    }

}
