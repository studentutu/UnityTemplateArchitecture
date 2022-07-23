# Example Automators


## Random Seed Automator
Sets the random seed for an Automated Run
```
using Unity.AutomatedQA;
using UnityEngine;

public class RandomSeedAutomatorConfig : AutomatorConfig<RandomSeedAutomator>
{
    public int seed;

}
public class RandomSeedAutomator : Automator<RandomSeedAutomatorConfig>
{
    public override void BeginAutomation()
    {
        base.BeginAutomation();
        Random.InitState(config.seed);
        EndAutomation();
    }
}
```