namespace Paynob.Patterns.Samples.Services
{
	public interface IScoreService
	{
		int GetScore( string name , int defaultScore = 0 );
		void SetScore( string name , int score = 0 );
	}
}