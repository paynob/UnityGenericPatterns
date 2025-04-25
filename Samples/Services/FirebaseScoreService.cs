namespace Paynob.Patterns.Samples.Services
{
	using UnityEngine;
    
	public class FirebaseScoreService : IScoreService
	{
		
		public int GetScore( string name , int defaultScore = 0 ) => throw new System.NotImplementedException( );
        public void SetScore( string name , int score = 0 ) => throw new System.NotImplementedException( );
	}
}
