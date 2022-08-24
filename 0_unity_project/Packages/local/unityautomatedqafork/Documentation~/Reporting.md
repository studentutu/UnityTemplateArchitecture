# Reporting
After each run of a recording (or recorded test), a report summarizing the results is saved to the application persistent data path. Reports from recordings run on the same machine as the Unity Editor can be accessed from the Recorded Playback window by pressing the "Show Report" button. Reports from recordings run on other devices can be accessed from the Unity application persistent data path on each device (the report could be accessed directly by reading from the device's file storage).

## Report Contents
A report shows device and test-run information in the summary box under the header. A piechart shows the number of passes and fails, along with "warnings" (tests that passed, but with console warnings during execution).

* Click on the piechart slices to filter results
* Click on individual tests or playbacks to see more details about that test or plaback. 
* Click on the "Logs" button to view the entire console log across all tests, including logs outside of the context of an executing test.
* Click on the "FPS Data" button to view a graph of framrates sampled over duration of test execution.
* Click on the "Heap Size" button to view a graph of heap size in megabytes sampled over duration of test execution.
* Screenshots are taken before and after each click or tap action in a playback
* Console logs, warnings, and errors (with full stacktrace available on click) are available for each test or playback as well.