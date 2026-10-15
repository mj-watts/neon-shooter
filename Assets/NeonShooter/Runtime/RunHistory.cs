using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonShooter.Unity
{
    [Serializable] public sealed class BattleRecord { public int score,wave,kills; }
    [Serializable] public sealed class BattleSave
    {
        public int bestScore,bestWave;
        public List<BattleRecord> battles=new List<BattleRecord>();
    }
    public sealed class RunHistory
    {
        const string Key="neon-shooter.unity.battles.v1";
        public BattleSave Data { get; private set; }
        public RunHistory()
        {
            try { Data=JsonUtility.FromJson<BattleSave>(PlayerPrefs.GetString(Key,"")); }
            catch(ArgumentException) { Data=null; }
            if(Data==null) Data=new BattleSave();
            if(Data.battles==null) Data.battles=new List<BattleRecord>();
            Data.battles.RemoveAll(b=>b==null || b.wave<0 || b.kills<0);
            if(Data.battles.Count>10) Data.battles.RemoveRange(10,Data.battles.Count-10);
        }
        public void Record(int score,int wave,int kills)
        {
            Data.bestScore=Data.battles.Count==0?score:Math.Max(Data.bestScore,score); Data.bestWave=Math.Max(Data.bestWave,wave);
            Data.battles.Insert(0,new BattleRecord { score=score,wave=wave,kills=kills });
            if(Data.battles.Count>10) Data.battles.RemoveAt(10);
            try { PlayerPrefs.SetString(Key,JsonUtility.ToJson(Data)); PlayerPrefs.Save(); }
            catch(PlayerPrefsException ex) { Debug.LogWarning("Battle remains available this session: "+ex.Message); }
        }
    }
}
