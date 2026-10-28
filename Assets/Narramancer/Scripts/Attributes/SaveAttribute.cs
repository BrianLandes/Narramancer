using System;

namespace Narramancer {
	/// <summary>
	/// Saves and restores this field with the story. Put it on behaviour state — a tween's target, whether a
	/// move is under way, the <see cref="Promise"/> a waiting verb resumes from — on any component whose object
	/// has a <see cref="Saveable"/>.
	/// <para>
	/// Component state (a transform's position, a text's contents) is the drivers' job, not this attribute's;
	/// see <see cref="IComponentSaver"/>. <see cref="Promise"/> and <see cref="NodeRunner"/> fields are filed in
	/// the story's promise and runner tables rather than the save table, so a waiting verb and the promise it
	/// waits on come back as one object.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class SaveAttribute : Attribute {

	}
}
