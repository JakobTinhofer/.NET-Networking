A base .NET Library which provides a unified framework for inter-app communication. Other libraries, such as LightBlueFox.Connect.Net, then offer various implementation of the abstract description of a connection provided by the base library.

## Features
- Provide a unified framework for apps where the exact nature of the connection is not of concern, greatly reducing reimplementation of e.g. basic networking code
- Offer high performance and reliability
- Allow for a great degree of extendibility, such as layering on encryption, p2p networking, protocols
- Extension libraries for various different types of connections, such as:
	- Networking (see LightBlueFox.Connect.Net)
	- WebSockets (idea)
	- I2C, Serial, etc. (idea)
	- Inter-Process
	- RF - connections
	- Exotic connections such as 3rd-party-platform communication (Discord, ...)

## Types
- #### Connection:
	The abstract base class that describes the minimum capabilities all connection implementations should offer, such as Reading and Writing. Also defines events and implements a few basic features such as read message queueing. 
- #### Util classes:
	A small collection of other Types that might be used when in any extending libraries, such as `MessageQueue`, `MessageStoreHandle` and `MessageArgs`.
