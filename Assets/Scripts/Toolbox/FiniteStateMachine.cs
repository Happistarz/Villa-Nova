using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Core
{
    public class FiniteStateMachine<T>
    {
        private readonly    List<State<T>> _states;
        [CanBeNull] private State<T>       _currentState;
        private readonly    State<T>       _defaultState;

        public State<T> CurrentState => _currentState;

        public FiniteStateMachine([CanBeNull] State<T> _initialState = null, [CanBeNull] List<State<T>> _states = null)
        {
            _defaultState = _initialState;
            _currentState = _initialState;

            this._states = _states ?? new List<State<T>>();
        }

        private State<T> TryGetNextState()
        {
            if (_currentState == null)
                return null;

            State<T> bestState = null;
            var      bestScore = 0f;

            foreach (var transition in _currentState.Transitions)
            {
                var score = transition.Evaluate(_currentState.Context);
                if (!(score > bestScore)) continue;
                bestScore = score;
                bestState = transition.To;
            }
            
            return bestState;
        }

        public void Update()
        {
            if (_currentState == null)
                return;

            var nextState = TryGetNextState();
            if (nextState != null && nextState != _currentState)
            {
                _currentState.Exit();
                _currentState = nextState;
                _currentState.Enter();
            }
            
            _currentState.Update();
        }

        public void Start()
        {
            _currentState?.Enter();
        }

        public void ForceState(State<T> _state)
        {
            if (_state == _currentState) return;
            _currentState?.Exit();
            _currentState = _state;
            _currentState?.Enter();
        }

        public void Reset()
        {
            _currentState?.Exit();
            _currentState = _defaultState;
            _currentState?.Enter();
        }
    }

    public class State<T>
    {
        protected internal T                      Context     { get; }
        protected          Action<T>              Behavior    { get; }
        protected          FiniteStateMachine<T>  SubFsm      { get; }
        public             List<Transition<T>>    Transitions { get; } = new();

        protected State(T _context, Action<T> _behavior = null, FiniteStateMachine<T> _subFsm = null)
        {
            Context  = _context;
            Behavior = _behavior;
            SubFsm   = _subFsm;
        }

        public virtual void Enter() { }

        public virtual void Exit() { }

        public virtual void Update()
        {
            if (Behavior != null)
                Behavior(Context);
            else
                SubFsm?.Update();
        }

        public virtual void Reset()
        {
            SubFsm?.Reset();
        }
    }

    public class Transition<T>
    {
        public readonly State<T>       To;
        public readonly Func<T, float> Condition;

        public Transition(State<T> _to, Func<T, float> _condition)
        {
            To        = _to;
            Condition = _condition;
        }

        public float Evaluate(T _context)
        {
            return Condition?.Invoke(_context) ?? 0f;
        }
    }
}