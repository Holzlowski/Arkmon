using NUnit.Framework;
using UnityEngine;

// Mute the test assembly only. Keep sources playing so timing/behaviour is unchanged.
[SetUpFixture]
public class SilentPlayModeTests
{
    private float previousVolume;

    [OneTimeSetUp]
    public void MuteTestOutput()
    {
        previousVolume = AudioListener.volume;
        AudioListener.volume = 0;
    }

    [OneTimeTearDown]
    public void RestoreOutput() => AudioListener.volume = previousVolume;
}
