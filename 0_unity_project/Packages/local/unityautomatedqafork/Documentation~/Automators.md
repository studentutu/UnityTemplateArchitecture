# Automators
An Automator is a script for controlling a segment of gameplay. Automators can be recordings, agents, or custom c# scripts.

Please email us at AutomatedQA@unity3d.com with any questions or suggestions about this feature!


## Automated Run
An Automated Run is used to configure a list of Automators to play in sequence.
- The `AutomatedRun` object links together recordings and custom C# scripts to automate gameplay. 
- Create an AutomatedRun with the `Create > Automated QA > Automated Run` menu
- Click `Run` in the inspector for the Automated Run to start playback.  
  
![](images/automated-run.gif) 
  
## Default Automators
See [Default Automators](AutomatorsIncluded.md) for a list of Automators provided out of the box with this package.

## Custom Automators
- Extend the `Automator` class to create a custom automator script.
- Extend the `AutomatorConfig` class for your Automator to expose it in the `AutomatedRun` inspector.

See [Example Automators](AutomatorExamples.md) for more information.
  

## CentralAutomationController
You can use the CentralAutomationController to automate gameplay programmatically.

An AutomatedRun asset can be passed to the CentralAutomationController to run it:
```
public AutomatedRun automatedRun;
void Start()
{
    CentralAutomationController.Instance.Run(automatedRun.config);
}
```

The CentralAutomationController can be also be configured programmatically: 
```
CentralAutomationController.Instance.AddAutomator<RecordedPlaybackAutomator>();
CentralAutomationController.Instance.Run();
```

Config data can also be created programmatically:
```
var config = new AutomatedRun.RunConfig();
config.automators.AddRange( new List<AutomatorConfig>{
    new LoadLevelAutomatorConfig()
    {
        scene = "SampleScene"
    },
    new RecordedPlaybackAutomatorConfig()
    {
        recordingFilePath = "Recordings/recording1.json"
    },
});

CentralAutomationController.Instance.Run(config);
```
