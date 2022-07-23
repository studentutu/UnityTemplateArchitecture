public static class RandomExtension
{
	public static int[] ShuffledArray(int length)
	{
		int[] shuffledIndices = new int[length];
		for (int i = 0; i < shuffledIndices.Length; i++)
		{
			shuffledIndices[i] = i;
		}

		for (int i = 0; i < length / 2; i++)
		{
			var random = UnityEngine.Random.Range(0, length);
			var temporal = shuffledIndices[random];
			shuffledIndices[random] = shuffledIndices[i];
			shuffledIndices[i] = temporal;
		}
		return shuffledIndices;
	}
}