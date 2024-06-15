# Narramancer
A node-based narrative system for Unity.

Full documentation at [the official website](https://narramancer.com/)

[Getting Started](https://narramancer.com/getting_started)

## Overview
Narramancer is a general solution behavior tree and save system. It acts as a scene independent table of the people and things in your game, as well as what they are doing.

## Nouns
The system assumes that the things in your game are ‘characters’ and provides a lot of convenience queries and operations to facilitate that, but at the same time allows for all kinds of things, including items, areas, ideas, etc. Because of this, the ‘things’ in Narramancer are referred to as Nouns.

## Verbs
The most powerful feature of Narramancer is the Verbs, which is a way of representing what the things in your game are doing. Verbs come in two types: verbs that ‘get’ information called ValueVerbs, and verbs that perform actions over time (possibly a fraction of a second, possibly minutes or hours) which are called ActionVerbs. Both kinds of verbs are graphs built up of nodes. Verbs can also have any number of inputs and outputs. You may have one verb for the entire game that handles the logic from one mode to another, or you may have a verb attached to each of the characters that controls only what they are doing, or some kind of combination.

Verbs can be thought of as functions in C# or any programming language, with input parameters and any number of output result values. ActionVerbs are similar to methods that are ‘async’ and ValueVerbs are executed instantaneously like typical, non-async methods.
Verbs can be built that use other Verbs, executing them over time or using them to process or retrieve information. Note that ValueVerbs cannot use ActionVerbs, but ActionVerbs can use ValueVerbs.

## Adjectives
Narramancer treats nouns similar to entities in Entity Component Systems, where a noun is simply a named container. Interesting data can then be added to a noun by simply attaching ‘components’ to it. In Narramancer, these kinds of components are referred to as ‘Adjectives’ and they come in three flavors: Properties, Stats, and Relationships.

### Properties
Properties can be added to or removed from a noun over the course of the game, and can be used to group nouns together, or flag a noun as having a certain quality or being treated in a certain way.

### Stats
Stats are number values that can be attached to a noun, then modified over the course of the game. Use stats to represent a noun’s health, mana, strength, or even as a running count of how many battles this noun has been in.

### Relationships
Relationships are unidirectional connections between two nouns. They can be added or removed over the course of the game, and are useful ways to represent things like how characters are related (eg: siblings), what has happened in the history between two characters, or how two areas are connected to each other.

## Variables / Blackboards
In addition to adjectives, nouns each have a general purpose ‘blackboard’, which is a keyed table of values that can be read and manipulated at runtime. This is useful for holding numbers, strings, images, or any other kind of data that a noun might need either from the start or added during the game.

In fact, there are various blackboards that can be used for storing values, including on a Verb or on the global state of the game.

## Save and Load
Saving in Narramancer takes the nouns plus any verbs and serializes them down into a save file, that can then be loaded at a later time. Verbs are built in such a way that even if it is in the middle of running an action, it remembers where it was and what it was doing, and can pick up right where it left off when re-loaded.
