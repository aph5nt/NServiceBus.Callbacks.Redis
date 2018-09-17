## Demo Instructions
- You must be running Redis locally on port 6379. I recommend just running it in docker, but as long as it's reachable on localhost, you're fine.
- Make sure that Reciever and Sender are set as startup projects and then run both at the same time.
- Observe the console windows for Sender. It is waiting for you to press enter before they send the commands to the Receiver. This is just to make sure the receiver is running first. Feel free to manipulate this any way that makes sense for testing.
- When the sample is finished, it shows that each instance of the bus only processed the message that it was expecting. This is seen by the difference in the instance IDs and message content.