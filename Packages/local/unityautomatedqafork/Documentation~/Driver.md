# Driver
The driver class provides advanced GameObject finding and interactions through the GameElement and ElementQuery classes.

The driver class offers simplified APIs to perform actions, with only the minimum information necessary. For example, a click can be performed with just a provided querySelector to find the GameObject. The Driver class also has APIs for Drags, SendKeys, and more.

Every action in the Driver class can be performed in two ways:
**Perform**: If a GameObject cannot be found, or the action cannot be performed, the test will fail with details on what went wrong.
**TryPerform**: The action is attempted, but failure to find a GameObject and inability to perform an action do not result in an error and test failure.

## GameElement
A GameElement component should be added to every interactable (or otherwise important) GameObject in a scene's hierarchy. When adding this class, fill out unique details about the element. These details should not change later in development, even if the GameObject's name changes, or its position in the scene hierarchy changes. If someone does edit these values, they know that they also need to update automation tests.

You can automatically add GameElements to interactable UI Elements in a scene by selecting `Automated QA Hub > Tools > Add Game Elements To Scene Objects`.

### ElementQuery
The ElementQuery class registers all GameElements in the scene, creating a much smaller and more performant pool of GameObjects than the SceneManager provides. This class also accepts Xpath-like & JQuery-like query selectors which are parsed and used to filter the GameElement pool based on the identifying properties and attributes assigned to them.

**Query String Options:**

	Letters, numbers, underscores (`_`) and spaces (` `) are the only valid characters for use in keys and attribute values.
	
`#` Indicates an Id as defined by an attached GameElement component on a GameObject.

`.` Indicates a Class as defined by an attached GameElement component on a GameObject.

`[]` Indicates a property or special search term.

	_Such as:_
	
	`[type=SOME_MONOBEHAVIOUR_COMPONENT]` Finds GameObjects with a monobehaviour/component of the requested type attached.
	
	`[text=SOME_TEXT]` Finds GameObjects with a Text or InputField component that have text equal to the requested value.

	`[text*=SOME_TEXT]` (Note the asterisk) Finds GameObjects with a Text or InputField component that have text _containing_ the requested value.
	
	`[CUSTOM_PROPERTY=CUSTOM_VALUE]` Finds GameObjects with any matching property added to an associated GameElement component. Both the property and its value are customizable.

	`[CUSTOM_PROPERTY*=CUSTOM_VALUE]` (Note the asterisk) Finds GameElements with any property containing the partial provided value. 


**Examples:**

`#SubmitButton` Finds GameObject with an attached GameElement component that has it's "Id" attribute set to "SubmitButton". Id values must ALWAYS be unique, making them the prime way to identify objects in a scene.

`.LargeButton` Finds GameObjects with an attached GameElement component that has a "class" attribute of "LargeButton".

`[type=Button]` Finds GameObjects that have a "Button" component attached to them.

`[text=Submit]` Finds GameObjects that have a "Text" component added to them or a child GameObject with text equal to "Submit".

`[text*=Submit]` Finds GameObjects that have a "Text" component added to them or a child GameObject with text containing OR equal to "Submit".

`.LargeBlue[type=toggle][text*=Submit]` Finds GameObjects that have a GameElement component with a class of "LargeBlue", a "Toggle" component on the GameObject, and a text component containing "Submit".