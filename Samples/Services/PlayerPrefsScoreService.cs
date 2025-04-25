namespace Paynob.Patterns.Samples.Services
{
	using UnityEngine;
    
	public class PlayerPrefsScoreService : IScoreService
	{
		private const string ScorePrefix = "SCORE_";
		public int GetScore( string name , int defaultScore = 0 ) {
            Debug.Log($"Get Score from PlayerPrefs for {name}"); 
            //PlayerPrefs.GetInt( ScorePrefix + name , defaultScore );
            return defaultScore; // Placeholder for actual PlayerPrefs call
        }
		public void SetScore( string name , int score = 0 ) => Debug.Log($"Set Score {score} on PlayerPrefs for {name}"); //PlayerPrefs.SetInt( ScorePrefix + name , score );
	}
}