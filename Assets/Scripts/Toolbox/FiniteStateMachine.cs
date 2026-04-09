using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Core
{
    public class FiniteStateMachine<T>
    {
        private readonly List<State<T>> _states;
        private readonly State<T>       _defaultState;

        [CanBeNull]
        public State<T> CurrentState { get; private set; }

        public FiniteStateMachine([CanBeNull] State<T> _initialState = null, [CanBeNull] List<State<T>> _states = null)
        {
            _defaultState = _initialState;
            CurrentState = _initialState;

            this._states = _states ?? new List<State<T>>();
        }

        private State<T> TryGetNextState()
        {
            if (CurrentState == null)
                return null;

            State<T> bestState = null;
            var      bestScore = 0f;

            foreach (var transition in CurrentState.Transitions)
            {
                var score = transition.Evaluate(CurrentState.Context);
                if (!(score > bestScore)) continue;
                bestScore = score;
                bestState = transition.To;
            }
            
            return bestState;
        }

        public void Update()
        {
            if (CurrentState == null)
                return;

            var nextState = TryGetNextState();
            if (nextState != null && nextState != CurrentState)
            {
                CurrentState.Exit();
                CurrentState = nextState;
                CurrentState.Enter();
            }
            
            CurrentState.Update();
        }

        public void Start()
        {
            CurrentState?.Enter();
        }

        public void ForceState(State<T> _state)
        {
            if (_state == CurrentState) return;
            CurrentState?.Exit();
            CurrentState = _state;
            CurrentState?.Enter();
        }

        public void Reset()
        {
            CurrentState?.Exit();
            CurrentState = _defaultState;
            CurrentState?.Enter();
        }
    }

    /// <summary>
    /// Base state with transitions and optional sub-FSM support
    /// </summary>
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

    /// <summary>
    /// Weighted transition between two states.
    /// </summary>
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