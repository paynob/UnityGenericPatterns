namespace Paynob.Patterns.States
{
    using System;
    internal class Transition
    {
        public State To { get; private set; }
        private readonly Func<bool> [ ] _conditions;

        public Transition( State to , params Func<bool> [ ] conditions ) {
            To = to;
            _conditions = new Func<bool> [ conditions.Length ];
            Array.Copy( conditions , _conditions , conditions.Length );
        }

        public bool Check() {
            for( var i = 0 ; i < _conditions.Length ; i++ ) {
                if( !_conditions [ i ].Invoke( ) ) {
                    return false;
                }
            }

            return true;
        }
    }
}
