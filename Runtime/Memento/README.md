# Memento Pattern
The Memento pattern is a software design pattern that provides the ability to restore an object to its previous state (undo via rollback).

## Intent
Without violating encapsulation, capture and externalize an object's internal state so that the object can be restored to this state later.

## Structure
* **Originator:** Creates a memento containing a snapshot of its current internal state.
* **Memento:** Stores the internal state of the Originator object.
* **Caretaker:** Responsible for the memento's safekeeping. Never operates on or examines the contents of a memento.
