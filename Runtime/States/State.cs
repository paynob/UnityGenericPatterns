namespace Paynob.Patterns.States
{
	using System;
	using System.Collections.Generic;

	public interface IState
	{
		void OnEnter();
		void OnExit();
		void Update();
	}
	internal class State
	{
		public readonly List<Transition> Transitions = new List<Transition>( );
		public Enum Name { get; set; }

		public Action? OnEnter, OnExit, Update;
		public State( Enum name ) => Name = name;


		public State CheckNext() {
			foreach( var transition in Transitions ) {
				if( transition.Check( ) ) {
					return transition.To;
				}
			}
			return this;
		}
	}
}
