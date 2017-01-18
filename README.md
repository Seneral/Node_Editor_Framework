###Expression Nodes
A collection of nodes that use reflection to dynamically parse objects of different types from text and convert them to any other, aswell as pass them into any system-level function.

<p align="center">
  <img alt="Node Editor Image" src="http://i.imgur.com/VHV3pql.jpg" width="60%"/>
</p>

This example shows how to integrate new dynamic types into the framework and adjust knob type after creation.
Installation is as easy as dropping the folder into your project with either Node Editor master or develop installed.

Note: The ActionNode, used to call any function in the system, currently does not work due to an internal error of UnityFunc. In the future I will make it use my other implementation for serializing actions &co., Serializable Action instead, which is generally far superior.