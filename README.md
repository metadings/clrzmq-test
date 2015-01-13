# ZeroMQ CLR namespace
Examples for [github.com/zeromq/clrzmq4](http://github.com/zeromq/clrzmq4)

Open this project in VC# Express on Windows, or in MonoDevelop on Linux.

Please setup the ZeroMQ assembly binding, either by adding the project ZeroMQ, or by adding the reference ZeroMQ.dll.

ZeroMQ is built in AnyCPU and running on both Windows and Linux, x86 and amd64.

You can run this project by command line `./ZeroMQ.Test.exe`

	Usage: ./ZeroMQ.Test.exe [--option=+] [--option=tcp://192.168.1.1:8080] <command> World Edward Ulrich

	Available [option]s:

	  --Frontend
	  --Backend

	Available <command>s:

	    PushPullDevice
	    Has
	    Version
	    RouterDealer
	    RequestReply
	    PushPull
	    PubSubDevice
	    PubSub
	    StreamDealer

Cool Projects 
---

StreamDealer

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.1:8080 StreamDealer World Edward Ulrich

which results in 

	Running...
	Please start your browser on tcp://192.168.123.10:8080 ...
	
You should have a look into the browser now...

--
	
PubSubDevice, on one machine

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.1:2772 --backend=tcp://192.168.1.1:3663 --server=++ PubSubDevice World Edward Ulrich
	
and on another machine

	./ZeroMQ.Test.exe --backend=tcp://192.168.1.1:3663 --client=+ PubSubDevice HI HA HO

that actually results in

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
