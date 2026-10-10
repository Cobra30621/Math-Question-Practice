namespace Game
{
    public static class MonsterStageNames
    {
        public static string Of(MonsterStage stage) => stage switch
        {
            MonsterStage.Baby => "寶寶",
            MonsterStage.Child => "小孩",
            _ => "大人"
        };
    }
}
