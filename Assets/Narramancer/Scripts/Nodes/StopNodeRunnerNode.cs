namespace Narramancer {
	[CreateNodeMenu("Flow/Stop")]
	public class StopNodeRunnerNode : RunnableNode {
		public override void Run(NodeRunner nodeRunner) {
			nodeRunner.StopAndReset();
		}
	}
}