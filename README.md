# ZeroMQ CLR namespace
Examples for [github.com/zeromq/clrzmq4](http://github.com/zeromq/clrzmq4)

Open this project in Visual C# on Windows or in MonoDevelop on Linux.

Please setup the ZeroMQ assembly binding, either by adding the project, or by adding the reference dll.

ZeroMQ is built in AnyCPU and running on both Windows (VC2010) and Linux (GNU C 4.8.2), x86 and amd64 running
through static readonly Delegate Field's.

You can run this project by command line `./ZeroMQ.Test.exe`

	Usage: ./ZeroMQ.Test.exe [--option=+] [--option=tcp://192.168.1.1:8080] <command> World Edward Ulrich

	Available [option]s:

	  --Backend
	  --Frontend

	Available <command>s:

	    Has
	    PubSub
	    PubSubDevice
	    PushPull
	    PushPullDevice
	    RequestReply
	    RouterDealer
	    StreamDealer
	    Version

ZeroMQ Projects 
---

StreamDealer
-

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:8080 StreamDealer World Edward Ulrich

which results in 

	Running...
	Please start your browser on tcp://192.168.1.10:8080 ...

Have a look into your browser!
	
PubSubDevice
-
On one machine (`192.168.1.10`)

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:2772 --backend=tcp://192.168.1.10:3663 --server=++ PubSubDevice World Edward Ulrich
	
and on another machine (`192.168.1.12`, beware the `10` and `12`)

	./ZeroMQ.Test.exe --backend=tcp://192.168.1.10:3663 --client=+ PubSubDevice HI HA HO

This results in

	Running...
	HA received 13.01.2015 07:07:52 World
	HI received 13.01.2015 07:07:52 World
	HO received 13.01.2015 07:07:52 World
	HI received 13.01.2015 07:07:52 Edward
	HO received 13.01.2015 07:07:52 Edward
	HA received 13.01.2015 07:07:52 Edward
	HA received 13.01.2015 07:07:52 Ulrich
	HI received 13.01.2015 07:07:52 Ulrich
	HO received 13.01.2015 07:07:52 Ulrich
	Cancelled...
