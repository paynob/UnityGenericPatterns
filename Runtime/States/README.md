# State Machine Pattern
- A generic state machine implementation (`StateMachine<T>`) that supports:
  - **State Transitions:** Define transitions between states with conditions.
  - **Event Binding:** Bind `OnEnter`, `OnExit`, and `Update` actions to states.
  - **IState Integration:** Link states to `IState` implementations for modular behavior.
  - **Dynamic State Addition:** Add and configure new states at runtime.

## Usage

### Without State Classes

```csharp
public enum MyStates
{
    Idle,
    Running,
    Dashing,
    Die
}

private StateMachine<MyStates> stateMachine;

void Start() {
    stateMachine = StateMachine<MyStates>.Create(MyStates.Idle);

    // Add transitions
    stateMachine.AddTransition(MyStates.Idle, MyStates.Running, () => Input.GetKey(KeyCode.Space));
    stateMachine.AddTransitionFromAny(MyStates.Die, () => !IsAlive );

    stateMachine.BindEvent( MyStates.Dashing , update: _Dash );
    stateMachine.BindEvent( MyStates.Die, onEnter: () => Debug.Log("Die") );
    stateMachine.BindEvent( MyStates.Idle, onExit: () => Debug.Log("Some Action called!") );

    stateMachine.StateChanged += OnStateChanged; 
}

void OnStateChanged( CharacterState previous , CharacterState current ) {
    // This method is called whenever the state changes. Use it to handle logic specific to state transitions.
 }

// Update the state machine
void Update() {
    stateMachine.Update();
}
```

### With State Classes

```csharp
public class CharacterIdleState : IState {
    private Character character;
    public CharacterIdleState(Character character) { this.character = character; }

    public void OnEnter( ) { }
    public void OnExit( ) { }
    public void Update( ) { }
}

public class CharacterRunningState : IState {
    private Character character;
    public CharacterRunningState(Character character) { this.character = character; }

    public void OnEnter( ) { }
    public void OnExit( ) { }
    public void Update( ) { }
}

public class CharacterDieState : IState {
    private Character character;
    public CharacterDieState(Character character) { this.character = character; }

    public void OnEnter( ) { }
    public void OnExit( ) { }
    public void Update( ) { }
}

private StateMachine<MyStates> stateMachine;

void Awake(){
    stateMachine = StateMachine<MyStates>.Create(MyStates.Idle);

    stateMachine.AddTransition(MyStates.Idle, MyStates.Running, () => Input.GetKey(KeyCode.Space));
    stateMachine.AddTransitionFromAny(MyStates.Die, () => !IsAlive );

    stateMachine.BindIState(MyStates.Moving, new CharacterRunningState( this ));
    stateMachine.BindIState(MyStates.Die, new CharacterDieState( this ));


    stateMachine.StateChanged += OnStateChanged; 

    // State Machine implements IEnumerator, so you can call it like this
    // It calls MoveNext() which is also called by stateMachine.Update() method
    StartCoroutine( stateMachine );
}

void OnStateChanged( CharacterState previous , CharacterState current ) {
    // This method is called whenever the state changes. Use it to handle logic specific to state transitions.
 }

// Coroutine started on Awake, so it's not necessary to call it manually
//void Update() {
//    stateMachine.Update();
//}
```