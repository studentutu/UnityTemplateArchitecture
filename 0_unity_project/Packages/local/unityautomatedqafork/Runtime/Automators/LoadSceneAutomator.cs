using System;
using System.Collections;
using Unity.AutomatedQA;
using UnityEngine.SceneManagement;

[Serializable]
public class LoadSceneAutomatorConfig : AutomatorConfig<LoadSceneAutomator>
{
    public string scene = "";
}

public class LoadSceneAutomator : Automator<LoadSceneAutomatorConfig>
{
    public override void BeginAutomation()
    {
        base.BeginAutomation();

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        var op = SceneManager.LoadSceneAsync(config.scene);
        while (!op.isDone)
        {
            yield return null;
        }
        EndAutomation();
    }
}