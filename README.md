# Paynob Patterns Library

A reusable library of design patterns for Unity, designed to simplify and enhance game development workflows. This library provides generic implementations of common patterns such as State Machine and Command, with a focus on flexibility and extensibility.

## Features

### 1. State Machine Pattern
- A generic state machine implementation (`StateMachine<T>`) that supports:
  - **State Transitions:** Define transitions between states with conditions.
  - **Event Binding:** Bind `OnEnter`, `OnExit`, and `Update` actions to states.
  - **IState Integration:** Link states to `IState` implementations for modular behavior.
  - **Dynamic State Addition:** Add and configure new states at runtime.

### 2. Command Pattern
- A generic command invoker (`CommandInvoker<T>`) that supports:
  - **Command Execution:** Execute commands on a receiver of type `T`.
  - **Undo/Redo Functionality:** Maintain a history of executed commands for undo/redo operations.

### 3. State Class
- A `State` class to represent individual states in the state machine:
  - Includes a list of transitions to evaluate conditions and determine the next state.
  - Supports configurable event actions (`OnEnter`, `OnExit`, `Update`).

## Installation

1. Clone or download the repository.
2. Add the `Runtime` folder to your Unity project.

## Usage

### State Machine Example
```csharp
var stateMachine = StateMachine<MyStates>.Create(MyStates.Idle);

// Add states
stateMachine.BindState(MyStates.Idle, new IdleState());
stateMachine.AddState(MyStates.Running, new RunningState());

// Add transitions
stateMachine.AddTransition(MyStates.Idle, MyStates.Running, () => Input.GetKey(KeyCode.Space));
stateMachine.AddTransition(MyStates.Running, MyStates.Idle, () => !Input.GetKey(KeyCode.Space));

// Update the state machine
void Update() {
    stateMachine.Update();
}