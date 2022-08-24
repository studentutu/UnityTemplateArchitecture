# Game Crawler
The Game Crawler is an agent designed to perform randomized actions in your application. It crawls from page to page, looking for possible ways to interact with GameObjects.

It records errors and warnings, and also determines when it has become stuck. It is designed to be used for both short and long periods of time to perform unpredictable actions that will catch errors that testers may not notice. Additionally, it can be used to perform soak tests to find errors encountered when an app is running for long periods of time. This may include memory leaks, crashes, and more.

## Automator Mode
Using an Automated Run, you can add a Game Crawler as an automator. This is useful if you want to navigate to a certain part of a game before activating a randomize crawl. From this mode, you can customize configurations for the run, uncluding making the automator run indefinitely (unless it becomes stuck). This is also how to run a GameCrawler from a CI process.

## Recording Mode
A GameCrawler can also be started from the Recorded Playback window. When stopped, a normal recording file will be generated, allowing one to then generate a C# test script from the randomized Game Crawler's recording.