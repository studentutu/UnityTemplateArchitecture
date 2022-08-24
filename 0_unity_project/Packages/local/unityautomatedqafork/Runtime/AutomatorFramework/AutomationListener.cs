#if AQA_USE_TMP
using TMPro;
#endif
using UnityEngine;
using UnityEngine.UI;
using static Unity.AutomatedQA.Listeners.GameListenerHandler;

namespace Unity.AutomatedQA.Listeners
{
	public class AutomationListener : MonoBehaviour
	{
		void Start()
		{
			if (TryGetComponent(out InputField input))
			{
				input.onValueChanged.AddListener(delegate
				{
					if (!IsInputSet(currentInput))
					{
						currentInput = new AutomationInput(input, Time.time, 0);
					}
					else
                    {
						currentInput = new AutomationInput(input, currentInput.StartTime, Time.time);
					}
				});
			}
#if AQA_USE_TMP
			else if (TryGetComponent(out TMP_InputField tmpInput))
			{
				tmpInput.onValueChanged.AddListener(delegate
				{
					if (!IsInputSet(currentInput))
					{
						currentInput = new AutomationInput(tmpInput, Time.time, 0);
					}
					else
					{
						currentInput = new AutomationInput(tmpInput, currentInput.StartTime, Time.time);
					}
				});
			}
#endif
		}
	}
}