namespace Paynob.Patterns.States
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public class StateMachine<T> : IEnumerator<T>, IEnumerable<T> where T : Enum
	{
		private readonly Dictionary<T , State> _states = new Dictionary<T , State>( );
		private State Current, Default;
		private State Any;

		public Action<T , T>? StateChanged;
		public Action<T>? OnEnterState, OnExitState;
		internal enum InternalStates { None, Any }
		public StateMachine( T defaultState ) {
			foreach( T element in Enum.GetValues( typeof( T ) ) ) {
				var s = new State( element );
				_states.Add( element , s );
			}
			Default = _states [ defaultState ];
			Current = Default;
			Any = new State( InternalStates.Any );
		}

		private void AddTransition( State from , State to , Func<bool> condition , params Func<bool> [ ] extraConditions ) {
			Func<bool> [ ] conditions = new Func<bool> [ extraConditions.Length + 1 ];
			conditions [ 0 ] = condition;
			for( var i = 1 ; i < conditions.Length ; i++ ) {
				conditions [ i ] = extraConditions [ i - 1 ];
			}
			from.Transitions.Add( new Transition( to , conditions ) );
		}
		#region PUBLIC_METHODS
		/// <summary>
		/// Creates a new State Machine for the enum type and sets defaultState as the initial state of the machine
		/// </summary>
		/// <param name="defaultState">The Initial state of the machine</param>
		/// <returns></returns>
		public static StateMachine<T> Create( T defaultState ) => new( defaultState );
		/// <summary>
		/// Sets an action to invoke whenever a transition occurs to state.
		/// </summary>
		/// <param name="state">State to which the transition occurs</param>
		/// <param name="action">Action to perform</param>
		public void SetOnEnterState( T state , Action action ) {
			_states [ state ].OnEnter = action;
		}
		/// <summary>
		/// Sets an action to invoke whenever a transition occurs from state.
		/// </summary>
		/// <param name="state">State from which the transition occurs</param>
		/// <param name="action">Action to perform</param>
		public void SetOnExitState( T state , Action action ) {
			_states [ state ].OnExit = action;
		}
		/// <summary>
		/// Sets an action to invoke meanwhile machine stays in this state.
		/// </summary>
		/// <param name="state">State in which the action occurs</param>
		/// <param name="action">Action to perform</param>
		public void SetStateUpdate( T state , Action action ) {
			_states [ state ].Update = action;
		}
		/// <summary>
		/// Maps events from the state of the machine to the methods of the IState interface
		/// </summary>
		/// <param name="state">State from the machine</param>
		/// <param name="istate">IState implementation whose methods will be called on each event type</param>
		public void BindIState( T state , IState istate ) {
			if (!_states.ContainsKey(state)) {
				throw new ArgumentException($"State {state} does not exist in the state machine.");
			}
			var s = _states [ state ];
			s.OnEnter = istate.OnEnter;
			s.OnExit = istate.OnExit;
			s.Update = istate.Update;
		}

		/// <summary>
		/// Maps events from the state of the machine to the methods passed as parameters
		/// </summary>
		/// <param name="state"></param>
		/// <param name="onEnter"></param>
		/// <param name="onExit"></param>
		/// <param name="update"></param>
		/// <exception cref="ArgumentException"></exception>
		public void BindEvent(T state, Action onEnter = null, Action onExit = null, Action update = null) {
			if (!_states.ContainsKey(state)) {
				throw new ArgumentException($"State {state} does not exist in the state machine.");
			}

			var s = _states[state];
			s.OnEnter = onEnter;
			s.OnExit = onExit;
			s.Update = update;
		}

		/// <summary>
		/// Adds a state to the state machine and binds it to an IState implementation.
		/// </summary>
		/// <param name="state">State to add</param>
		/// <param name="istate">IState implementation to bind to the state</param>
		/// <exception cref="ArgumentException">Thrown if the state already exists in the state machine.</exception>
		public void AddState(T state, IState istate){
			if (_states.ContainsKey(state)) {
				throw new ArgumentException($"State {state} already exists in the state machine.");
			}
			
			_states.Add( state, new State( state ) );
			BindIState(state, istate);
		}

		/// <summary>
		/// Adds a transition between to states given one or more Func{bool}.<br/>
		/// <i>from</i> will check transitions in the order they was added.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="condition"></param>
		/// <param name="extraConditions"></param>
		public void AddTransition( T from , T to , Func<bool> condition , params Func<bool> [ ] extraConditions ) {
			AddTransition( _states [ from ] , _states [ to ] , condition , extraConditions );
		}

		/// <summary>
		/// Adds a transition from AnyState to given state given one or more Func{bool}
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="condition"></param>
		/// <param name="extraConditions"></param>
		public void AddTransitionFromAny( T to , Func<bool> condition , params Func<bool> [ ] extraConditions ) {
			AddTransition( Any , _states [ to ] , condition , extraConditions );
		}
		#endregion PUBLIC_METHODS

		IEnumerator IEnumerable.GetEnumerator() => this;
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;

		T IEnumerator<T>.Current => (T)Current.Name;

		object IEnumerator.Current => Current.Name;
		public bool MoveNext() {
			var old = Current;
			var next = Any.CheckNext( );

			Current = Any == next ? Current.CheckNext( ) : next;

			if( old != Current ) {
				old.OnExit?.Invoke( );
				OnExitState?.Invoke( (T)old.Name );
				Current.OnEnter?.Invoke( );
				OnEnterState?.Invoke( (T)Current.Name );
				StateChanged?.Invoke( (T)old.Name , (T)Current.Name );
			}
			Current.Update?.Invoke( );
			return true;
		}
		void IEnumerator.Reset() => Current = Default;
		void IDisposable.Dispose() { }

		public void Update() {
			MoveNext( );
		}
	}
}