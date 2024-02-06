namespace ChessTesting
{
	public abstract class AISettings {
		public bool endlessSearchMode;		
		public MoveGenerator.PromotionMode promotionsToSearch;
		public SearchDiagnostics diagnostics;
	}
}