C# Api Example
==============

This example shows how to use the Zyncro REST API from C#.

We use [DevDefined OAuth](http://code.google.com/p/devdefined-tools/wiki/OAuth "DevDefined OAuth") as the OAuth client library.

`ZyncroApiSample.cs` shows how to get a validated Access Token and invoke two API services. One to get the microblogging and another to post a new message.

In a real world app you can store the Access Token and reuse it.