# ZeroMQ CLR namespace
Examples for [github.com/zeromq/clrzmq4](http://github.com/zeromq/clrzmq4)

Open this project in Visual C# on Windows or in MonoDevelop on Linux.

Please setup the ZeroMQ assembly binding, either by adding the project, or by adding the reference dll.

ZeroMQ is built in AnyCPU, running on Windows (VC2010) and Linux (GNU C 4.8.2) on x86 and amd64.   
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

All projects have now a `--monitor` option, which enables ZMonitor usage.

StreamDealer
-

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:8080 StreamDealer World Edward Ulrich

which results in 

	Running...
	Please start your browser on tcp://192.168.1.10:8080 ...

Have a look into your browser!

RouterDealer
-

First machine (`192.168.1.10`)

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:2772 --backend=tcp://192.168.1.10:3663 --server=++ RouterDealer World Edward Ulrich
	
Second machine (`192.168.1.12`, beware the `10` and `12`)

	./ZeroMQ.Test.exe --backend=tcp://192.168.1.10:3663 --client=+ RouterDealer HA HE HI HO HU
	
Results:

	World says hello to HA
	Edward says hello to HE
	Ulrich says hello to HI
	World says hello to HO
	Edward says hello to HU
	Ulrich says hello to HA

	
PubSubDevice
-

First machine (`192.168.1.10`)

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:2772 --backend=tcp://192.168.1.10:3663 --server=++ PubSubDevice World Edward Ulrich
	
Second machine (`192.168.1.12`, beware the `10` and `12`)

	./ZeroMQ.Test.exe --backend=tcp://192.168.1.10:3663 --client=+ PubSubDevice HI HA HO

Results:

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

	
PushPullDevice
-

First machine (`192.168.1.10`)

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:2772 --backend=tcp://192.168.1.10:3663 --server=++ PushPullDevice World Edward Ulrich
	
Second machine (`192.168.1.12`, beware the `10` and `12`)

	./ZeroMQ.Test.exe --frontend=tcp://192.168.1.10:2772 --client=+ PushPullDevice HA HE HI HO HU

Results:

	Running...
	HA said hello to Edward!
	HU said hello to World!
	HO said hello to Ulrich!
	HE said hello to Edward!
	HI said hello to World!
	Cancelled...
