using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace Sui.Machine
{
    /*
    In English, so they don’t go whining to me.
    -- Variables:
    starts: m_ = public, Recommended to use getters and setters
    starts: _ = private, NOT local
    starts: lowercase = private, local
    starts: "p" (lowercase) = local incoming parameter
    starts: uppercase = public
    starts: Z_ = Designed for the Unity Editor 
    all uppercase: is constant
    -- Functions & Class:
    starts: uppercase = public
    starts: lowercase = private
    starts: __ = public, Not recommended
    -- All:
    contain: _internal = divided not used final programer
    */



    public interface IMachineState
    {
        IState CurrentState { get; set; }
        int Count { get; }

        int Identifier { get; set; }

        O GetOwner<O>() where O : class;

        void UpdateTransitions();
        //internal void UpdateTransitions_internal();

        // Métodos públicos de MachineState
        IState this[int pIndex] { get; set; }
        int this[IState pState] { get; set; }
        List<IState> PossibleStates { get; set; }
        List<IState> this[List<IState> pPossibleStates] { get; set; }
        string[] NamesStates { get; }
        string NameStates2 { get; }
        bool Active { get; set; }
        int IndexState { get; }
        event Action<IState> OnStateChanged;

        //void manejarEntradaEstado(IState eNovoEstado);
        //void manejarSalidaEstado(IState eNovoEstado);

        (IState newS, IState oldS) ChangeState(int pNovoId, ChangeParam? pParam = null);
        (IState newS, IState oldS) ChangeState(string pNovoName, ChangeParam? pParam = null);
        (IState newS, IState oldS) ChangeState<S>(ChangeParam? eParam = null);

        (IState newS, IState oldS) ForceChangeState(int pNovoId, ChangeParam? pParam = null);
        (IState newS, IState oldS) ForceChangeState(string pNovoName, ChangeParam? pParam = null);
        (IState newS, IState oldS) ForceChangeState<S>(ChangeParam? pParam = null);

        void AgregarEstadoPersistente(IState pState);
        void RemoverEstadoPersistente(IState pState);
        //void StopCoroutine(ref Coroutine pCoroutine);
        //Coroutine StartCoroutine(IEnumerator pEnumerator);
        void Exit();
        bool AddTransition(Func<bool> pCondition, int pStateDestine);
        bool AgregarTransicion(Func<bool> pCondition, int pStateDestine);
        int GetIndex(IState pState);
        int GetIndex(string pName);
        int GetIndex<S>();
        IState GetState(int pIndex);
        string GetNameState(int pIndex);
        S CreateState<S>() where S : IState;
        void CreateStateAutoAdd<S>() where S : IState;
        List<IState> ChangeListSates(List<IState> pNovoList);
        void ClearImmediate(List<IState> pSpecificExclusions = null);
        void Clear(List<IState> pSpecificExclusions = null);
        void Rm(List<IState> pDeletList);
        void Rm(IState pState);
    }



    // TODO: Esta creado lo nombres, ahora toca Implentarlo en los demas sistemas (Usar nombres en vez del indice).
    // NOTA: que nombres? no me acuerdo, me falta contexto XD.
    // TODO: Poder borrar estados de la lista de estados posibles.
    // TODO: Traducir todos los summary a ambos idiomas (Español e Ingles).
    // TODO: Un SetSate que fuerce la salida sin pasar por Exit pero si por Enter del siguiente.

    // TODO: Hacer un sistema de Identificadores para las maquinas de estados.
    // TODO: Que 'GestionadorMachineState' se encarge de suministrar los identificadores de las maquinas de estados.
    // TODO: Que los estados guarden el Id de la maquina.
    // TODO: que un estado al cambiar de maquina se actualicen el id de la maquina y el id del estado.

    // TODO: Gestionar de alguna manera cuando cualquiera haga .Add sobre _estadosPosibles.

    // TODO: ver si es viable cambiar el tipo de <O> a IState.

    /*
        // Problema: llama al Add de List en vez de ListState.
        var _estadoMoviendose = machineState.CreateState<EstadoMoviendose>();
        machineState.PosibleStates.Add(_estadoMoviendose);
     */

    public enum ChangeStateOptions : byte
    {
        None = 0,
        SkipEnter = 1 << 0, // 0001
        SkipExit = 1 << 1, // 0010
    }

    // Permitir saltarse los Enters.
    // Permitir saltarse los Exits.
    /// <summary>
    /// 
    /// </summary>
    public struct ChangeParam
    {
        public ChangeStateOptions? NewOptions { get; set; }
        public ChangeStateOptions? OldOptions { get; set; }


        public ChangeParam(ChangeStateOptions NewOptions, ChangeStateOptions OldOptions)
        {
            this.NewOptions = NewOptions;
            this.OldOptions = OldOptions;
        }
        public ChangeParam(ChangeStateOptions pOptions)
        {
            this.NewOptions = pOptions;
            this.OldOptions = pOptions;
        }


        public static ChangeStateOptions None()
        {
            return ChangeStateOptions.None;
        }
        public static ChangeStateOptions SkipEnter()
        {
            return ChangeStateOptions.SkipEnter;
        }
        public static ChangeStateOptions SkipExit()
        {
            return ChangeStateOptions.SkipExit;
        }
        public static ChangeStateOptions SkipBoth()
        {
            return ChangeStateOptions.SkipEnter | ChangeStateOptions.SkipExit;
        }
    }


    /// <summary>
    /// --------------------------------------------------------------
    /// <br />
    /// Maquina de Estados  
    /// <br />
    /// --------------------------------------------------------------
    /// </summary>
    public partial class MachineState<O> : IMachineState where O : class
    {
        // ***********************( ListState )*********************** //
        // TODO: sacar al namespace para que cualquiero pueda usarlo y asegurar que no haya problemas al hacerlo.
        // TODO: Permitir que al modificarlo cualquier Maquina con esta lista tambien cambie.
        private class ListState : List<IState>
        {
            private readonly MachineState<O> _machineState;

            // Constructores.
            public ListState(MachineState<O> machineState)
            {
                _machineState = machineState;
            }

            public ListState(MachineState<O> machineState, IEnumerable<IState> collection) : base(collection)
            {
                _machineState = machineState;
                foreach (var item in collection)
                {
                    _machineState.GestionarEstado(item);
                }
            }

            // Getters y Setters.
            public IEnumerable<IState> Asignacion
            {
                get
                {
                    return new List<IState>(this);
                }
                set
                {
                    if (value == null)
                    {
                        Debug.LogError($"({_machineState._host.GetType().Name}->MachineState): the collection is null.");
                        return;
                    }
                    base.Clear();
                    foreach (var item in value)
                    {
                        this.Add(item);
                    }
                }
            }

            // Metodos.
            public new void Add(IState item)
            {
                base.Add(item);
                _machineState?.GestionarEstado(item);
                item.OnEnteringPossibleStates_internal(_machineState);
            }

            public new void AddRange(IEnumerable<IState> collection)
            {
                foreach (var item in collection)
                {
                    this.Add(item);
                }
            }
        }

        // ***********************( Variables/Declaraciones )*********************** //
        private IState _currentState { get; set; } = null; // Representa el estado actual de la maquina.

        private ListState _possibleStates { get; set; }
        private ListState _persistentStates;

        private Dictionary<Func<bool>, IState> _transitions = new();
        private object _host;
        private O _owner; // Script donde fue instanciada la maquina de estados.

        // --- Control.
        private bool _enable = true;

        // --- Gestion.
        private HashSet<int> _allStates = new();
        private int _creschendoId = 0;

        private int _identifier;

        // ***********************( Getter, Setters e Indesxadores )*********************** //
        // TODO: Si llega un State desconocido, que se cree automaticamente y se añada a la lista de estados posibles.
        public IState CurrentState
        {
            get
            {
                //if (_estadoActual == null)
                //    Debug.Log($"({_go.GetType().Name}->MachineState): return State Null.");

                return _currentState;
            }
            set
            {
                if (_currentState == value)
                    return;

                manejarCambioEstado(value);

                UpdateTransitions_internal();
            }
        }

        public S GetCurrentState<S>() where S : class => CurrentState as S;
        public IState GetCurrentState() => CurrentState;


        int IMachineState.Identifier
        {
            set { _identifier = value; }
            get { return _identifier; }
        }


        /// <summary>
        /// get: Devuelve el estado en la posicion del indice.<br />
        /// set: Guarda el estado en la posicion del indice.<br />
        /// </summary>
        /// <param name="_indice_i"></param>
        /// <returns></returns>
        public IState this[int _indice_i]
        {
            get
            {
                if (_indice_i < 0 || _indice_i >= _possibleStates.Count)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): El índice de estado es inválido.");
                    return null;
                }
                return _possibleStates[_indice_i];
            }
            set
            {
                if (_indice_i < 0)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): El índice de estado es inválido.");
                    return;
                }
                _possibleStates[_indice_i] = value;
            }
        }

        /// <summary>
        /// get: Devuelve el indice del estado.<br />
        /// set: Guarda el estado en la posicion del indice.<br />
        /// </summary>
        /// <param name="_estado"></param>
        /// <returns></returns>
        public int this[IState _estado]
        {
            get
            {
                if (_estado == null)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): El estado proporcionado es null.");
                    return -1;
                }
                return GetIndex(_estado);
            }
            set
            {
                if (_estado == null)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): El estado proporcionado es null.");
                    return;
                }
                _possibleStates[value] = _estado;
            }
        }

        /// <summary>
        /// get: Devuelve la lista de estados posibles.<br />
        /// set: Sustituye la lista de estados posibles.<br />
        /// </summary>
        public List<IState> PossibleStates
        {
            get
            {
                return _possibleStates;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): La lista de estados posibles es null.");
                    return;
                }

                if (value.Count == 0)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): La lista de estados posibles no puede estar vacía.");
                    return;
                }

                //annadirEstadosTodos(value);

                _possibleStates.Asignacion = value;
            }
        }

        // TODO: Comprobar si el get fufa.
        /// <summary>
        /// get: Devuelve la lista de estados posibles.<br />
        /// set: Sustituye la lista de estados posibles.<br />
        /// </summary>
        public List<IState> this[List<IState> _estadosPosibles_obj]
        {
            get
            {
                return _possibleStates;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): The passed list of states is null.");
                    return;
                }

                if (value.Count == 0)
                {
                    Debug.LogError($"({_host.GetType().Name}->MachineState): The passed list of states cannot be empty.");
                    return;
                }

                //annadirEstadosTodos(value);

                _possibleStates.Asignacion = value;
            }
        }

        public string[] NamesStates
        {
            get
            {
                string[] nombres = new string[_possibleStates.Count];
                for (int i = 0; i < _possibleStates.Count; i++)
                {
                    if (_possibleStates[i] != null)
                    {
                        nombres[i] = _possibleStates[i].GetType().Name;
                    }
                }
                return nombres;
            }
        }

        public string NameStates2
        {
            get
            {
                return string.Join(", ", PossibleStates.Select(e => e.GetType().Name));
            }
        }

        /// <summary>
        /// get: Devuelve si la máquina de estados está activa.<br />
        /// set: Activa o desactiva la máquina de estados.<br />
        /// NOTA: No pasa por Enter ni Exit.
        /// </summary>
        public bool Active
        {
            get { return _enable; }
            set
            {
                if (_currentState != null)
                {
                    if (value == true)
                        _currentState.enabled = true;
                    else
                        _currentState.enabled = false;

                    _enable = value;
                }
                else
                    Debug.Log("(MachineState): State is NULL.");
            }
        }

        public int IndexState
        {
            get { return GetIndex(CurrentState); }
        }


        public int Count
        {
            get { return _possibleStates.Count; }
        }


        public O Owener
        {
            get { return _owner; }
            set { _owner = value; }
        }

#pragma warning disable CS0693 // wow.
        public O GetOwner<O>() where O : class
#pragma warning restore CS0693
        {
            return Owener as O;
        }

        // ***********************( Eventos )*********************** //
        public event Action<IState> OnStateChanged;



        // ***********************( Metodos de Estados )*********************** //
        void manejarCambioEstado(IState pNovo)
        {
            manejarSalidaEstado(pNovo);

            var before = _currentState;
            _currentState = pNovo;
            OnStateChanged?.Invoke(_currentState);

            //if (!_go.activeInHierarchy)
            //{
            //    Debug.LogWarning($"({_go.GetType().Name}->MachineState): The GameObject is: Disable.");
            //}

            manejarEntradaEstado(before);
        }
        void manejarSalidaEstado(IState pNovo)
        {
            if (_currentState != null)
            {
                if ((_currentState.ChangeOptions & ChangeStateOptions.SkipExit) == 0)
                {
                    _currentState?.ManagerExit_internal(this);
                    if (!_currentState.ChangeExit_internal(pNovo))
                    {
                        _currentState?.Exit();
                    }
                    _currentState?.ManagerAfterExit_internal(this);
                }

                _currentState.enabled = false;
                _currentState.ChangeOptions = ChangeStateOptions.None;
            }
        }
        void manejarEntradaEstado(IState pBefore)
        {
            if (_currentState != null)
            {
                _currentState.enabled = true;

                if ((_currentState.ChangeOptions & ChangeStateOptions.SkipEnter) == 0)
                {
                    _currentState?.ManageEnter_internal(this);
                    if (!_currentState.ChangeEnter_internal(pBefore))
                    {
                        _currentState?.Enter();
                    }
                    _currentState?.ManageAfterEnter_internal(this);
                }
            }
        }

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual de la máquina de estados.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state of the state machine.<br />
        /// </summary>
        /// <param name="eNovoId">Es: Posicion en int del 'estadosPosibles' <br /> En: Position in int of 'PossibleStates'</param>
        public (IState newS, IState oldS) ChangeState(int eNovoId, ChangeParam? eParam = null)
        {
            var olds = CurrentState;
            if (olds != null)
                olds.ChangeOptions = eParam?.OldOptions ?? ChangeStateOptions.None; // Excepcion Null

            if (!cambiarEstado(eNovoId, out var novoState, eParam))
                return (null, olds);

            return (novoState, olds);
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual al respectivo nombre.<br />
        /// Añadalo primero a la lista o utilice 'CreateStateAutoAdd'.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state to the respective name.<br />
        /// Add it first to the list or use 'CreateStateAutoAdd'.<br />
        /// </summary>
        /// <param name="eNovoName">Es: nombre del estado <br />En: name of state</param>
        /// <returns>Es: Retorna el nuevo estado cambiado <br />En: Returns the new changed state</returns>
        public (IState newS, IState oldS) ChangeState(string eNovoName, ChangeParam? eParam = null)
        {
            var olds = CurrentState;

            if (string.IsNullOrEmpty(eNovoName))
            {
                Debug.LogError("(MachineState -> ChangeState): The name of the new state is null or empty.");
                return (null, olds);
            }
            return ChangeState(GetIndex(eNovoName), eParam);
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual al respectivo.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state to the respective.<br />
        /// </summary>
        /// <returns>Es: Retorna el nuevo estado cambiado <br />En: Returns the new changed state</returns>
        /// <typeparam name="S">Es: Tipo del estado a cambiar <br />En: Type of the state to change</typeparam>
        public (IState newS, IState oldS) ChangeState<S>(ChangeParam? eParam = null)
        {
            return ChangeState(GetIndex(typeof(S).Name), eParam);
        }

        private bool darmeNuevoEstado(int eNuevoEstado, out IState eNovo)
        {
            eNovo = null;
            if (eNuevoEstado == -1)
                return true;

            if (eNuevoEstado >= 0 || eNuevoEstado < _possibleStates.Count)
                eNovo = _possibleStates[eNuevoEstado];

            if (eNovo == null)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState -> (internal)ChangeState): Attempt to change state null");
                return false;
            }

            return true;
        }

        private bool cambiarNuevoEstado(int eNuevoEstado, out IState eSalida, ChangeParam? eParam)
        {
            eSalida = null;

            if (_possibleStates == null)
                return false;

            if (eNuevoEstado < -1 || eNuevoEstado >= _possibleStates.Count)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState -> (internal)ChangeState): The index of the new state is out of range.");
                return false;
            }

            if (!darmeNuevoEstado(eNuevoEstado, out IState _posibleNovoEstado))
            {
                return false;
            }

            if (_posibleNovoEstado != null)
                _posibleNovoEstado.ChangeOptions = eParam?.NewOptions ?? ChangeStateOptions.None;

            eSalida = _posibleNovoEstado;
            return true;
        }

        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        /// <param name="eNuevoEstado">'int' del estado a cambiar</param>
        /// <param name="_salidaNovo">nuevo estado al cambiado</param>
        /// <returns>false si no llego a cambiar</returns>
        private bool cambiarEstado(int eNuevoEstado, out IState _salidaNovo, ChangeParam? eParam)
        {
            _salidaNovo = null;

            if (!cambiarNuevoEstado(eNuevoEstado, out IState lNovo, eParam))
            {
                return false;
            }

            CurrentState = lNovo;
            _salidaNovo = lNovo;
            return true;
        }

        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        /// <param name="eNuevoEstado">'int' del estado a cambiar</param>
        /// <param name="_salida">nuevo estado al cambiado</param>
        /// <returns>false si no llego a cambiar</returns>
        private bool cambiarEstadoForzado(int eNuevoEstado, out IState _salida, ChangeParam? eParam)
        {
            _salida = null;

            if (!cambiarNuevoEstado(eNuevoEstado, out IState lNovo, eParam))
            {
                return false;
            }

            manejarCambioEstado(lNovo);
            UpdateTransitions_internal();
            _salida = CurrentState;
            return true;
        }


        public (IState newS, IState oldS) ForceChangeState(int eNovoId, ChangeParam? eParam = null)
        {
            var olds = CurrentState;
            if (olds != null)
                olds.ChangeOptions = eParam?.OldOptions ?? ChangeStateOptions.None;

            if (!cambiarEstadoForzado(eNovoId, out IState lNovo, eParam))
                return (null, olds);

            return (lNovo, olds);
        }

        public (IState newS, IState oldS) ForceChangeState(string eNovoName, ChangeParam? eParam = null)
        {
            var olds = CurrentState;

            if (string.IsNullOrEmpty(eNovoName))
            {
                Debug.LogError("(MachineState -> ForceChangeState): The name of the new state is null or empty.");
                return (null, olds);
            }
            return ForceChangeState(GetIndex(eNovoName), eParam);
        }

        public (IState newS, IState oldS) ForceChangeState<S>(ChangeParam? eParam = null)
        {
            return ForceChangeState(GetIndex(typeof(S).Name), eParam);
        }


        internal void GestionarEstado(IState e_estado)
        {
            if (e_estado != null)
            {
                if (e_estado.Identificador <= 0)
                    e_estado.Identificador = soliticeId_internal();
                if (!this._allStates.Contains(e_estado.Identificador))
                    this._allStates.Add(e_estado.Identificador);
            }
        }

        // NOTA: no se si dejarlo pues hace lo mismo que 'PosibleStates'.
        public List<IState> ChangeListSates(List<IState> _novoLista)
        {
            if (_novoLista == null)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): La nueva lista de estados posibles es nula.");
                return null;
            }
            if (_novoLista.Count == 0)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): La nueva lista de estados posibles esta vacía.");
                return null;
            }

            CurrentState = null;
            PossibleStates = _novoLista;

            return PossibleStates;
        }

        /*
        // ---( Asincronos )--- //
        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public async Task CambiarEstadoAsync(IState nuevoEstado)
        {
            if (State != null)
            {
                await State.ExitAsync();
                State.enabled = false;
            }

            State = nuevoEstado;
            //State.MachineState = this;
            State.enabled = true;
            await State.EnterAsync();
        }
        */

        // ---( Persistentes )--- //
        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void AgregarEstadoPersistente(IState estado)
        {
            if (!_persistentStates.Contains(estado))
            {
                _persistentStates.Add(estado);
                //estado.MachineState = this;
                estado.enabled = true;
                estado.Enter();
            }
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void RemoverEstadoPersistente(IState estado)
        {
            if (_persistentStates.Contains(estado))
            {
                estado.Exit();
                estado.enabled = false;
                _persistentStates.Remove(estado);
            }
        }

        // ***********************( Metodos Transicionarios )*********************** //
        //public void StopCoroutine(ref Coroutine pCoroutine)
        //{
        //    if (_go == null)
        //    {
        //        Debug.LogError("(Null->MachineState): The host GameObject is null in StopCoroutine.");
        //        return;
        //    }
        //    _go.GetComponent<O>().StopCoroutine(pCoroutine);
        //    pCoroutine = null;
        //}
        //public Coroutine StartCoroutine(IEnumerator pEnumerator)
        //{
        //    if (_go == null)
        //    {
        //        Debug.LogError("(Null->MachineState): The host GameObject is null in StartCoroutine.");
        //        return null;
        //    }
        //    return _go.GetComponent<O>().StartCoroutine(pEnumerator);
        //}


        // ***********************( Metodos Forzar )*********************** //
        public void Exit()
        {
            CurrentState = null;
        }


        // ***********************( Metodos Gestion Ides )*********************** //
        private int soliticeId_internal()
        {
            return ++_creschendoId;
        }


        // ***********************( Metodos Update )*********************** //
        public void Update()
        {
            if (CurrentState != null && CurrentState is IStateLittle noMono)
                noMono.Update();
        }
        public void PhysicsUpdate()
        {
            if (CurrentState != null && CurrentState is IStateLittle noMono)
                noMono.PhysicsUpdate();
        }


        // ***********************( Metodos Transiciones )*********************** //
        public bool AddTransition(Func<bool> condition, int stateDestine)
        {
            return this.AgregarTransicion(condition, stateDestine);
        }

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Agrega una transición a la máquina de estados. <br /> 
        /// Una transicion es una condición que, al cumplirse, cambia el estado actual de la máquina.<br />
        /// </summary>
        /// <param name="e_condicion_fb">Es la condicion en lamda para cambiar '() => _parar_b == true'</param>
        /// <param name="e_estadoDestino_i">Estado al que cambiara pasando el int de la posicion de 'estadosPosibles'</param>
        public bool AgregarTransicion(Func<bool> e_condicion_fb, int e_estadoDestino_i)
        {
            _transitions ??= new();

            if (e_estadoDestino_i < 0 || e_estadoDestino_i >= _possibleStates.Count)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): The destination state index is invalid in AddTransition.");
                return false;
            }

            _transitions[e_condicion_fb] = _possibleStates[e_estadoDestino_i];
            //Debug.Log($"Transición agregada: {estadosPosibles[estadoDestino].GetType().Name}");
            return true;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Actualiza las transiciones de la máquina de estados.
        /// </summary>
        public void UpdateTransitions()
        {
            // Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAA");
            UpdateTransitions_internal();
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void UpdateTransitions_internal()
        {
            if (_transitions == null)
                return;

            foreach (var transicion in _transitions)
            {
                if (transicion.Key.Invoke())
                {
                    ChangeState(GetIndex(transicion.Value));
                    break;
                }
            }
        }

        // ***********************( Indices )*********************** //
        public int GetIndex()
        {
            return GetIndex(CurrentState);
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Obtiene el índice del estado en la lista de estados posibles.<br />
        /// ___________________( English )___________________<br />
        /// Gets the index of the state in the list of possible states.<br />
        /// </summary>
        /// <param name="estado">Es: Estado del que se quiere sacar el indice <br /> En: State from which to get the index</param>
        /// <returns>Retona un int del indice</returns>
        public int GetIndex(IState estado)
        {
            if (estado == null)
            {
                Debug.Log($"({_host.GetType().Name}->GetIndex): return -1 for null state.");
                return -1;
            }

            int indice = _possibleStates.IndexOf(estado);
            if (indice == -1)
            {
                Debug.LogWarning($"({_host.GetType().Name}->GetIndex): El estado {estado.GetType().Name} no se encuentra en la lista de estados posibles.");
            }

            return indice;
        }

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Obtiene el índice del estado en la lista de estados posibles.<br />
        /// ___________________( English )___________________<br />
        /// Gets the index of the state in the list of possible states.<br />
        /// </summary>
        /// <param name="pNameState">Es: nombre del estado <br /> En: name of state</param>
        /// <returns>Es: Retona un int del indice<br />En: Returns an int of the index</returns>
        public int GetIndex(string pNameState)
        {
            if (string.IsNullOrEmpty(pNameState))
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): The name IsNullOrEmpty is true.");
                return -1;
            }

            foreach (var state in _possibleStates)
            {
                string nameState = state.GetType().Name;
                if (pNameState.Equals(nameState))
                {
                    return _possibleStates.IndexOf(state);
                }
            }

            Debug.LogWarning($"({_host.GetType().Name}->MachineState): The state {pNameState} not found on the list of states.");
            return -1;
        }

        public int GetIndex<S>()
        {
            return GetIndex(typeof(S).Name);
        }

        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        public IState GetState(int pIndex)
        {
            return _possibleStates[pIndex];
        }

        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        public string GetNameState(int pIndex)
        {
            return _possibleStates[pIndex].GetType().Name;
        }

        // ***********************( Serealizacion )*********************** //
        // TODO: Implementar un sistema de serialización para guardar la máquina de estados.



        // ***********************( Funciones Constructores )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Crea un nuevo estado de tipo S y lo inicializa con la dependencia proporcionada.<br />
        /// LLama al metodo Init() del estado.<br />
        /// -----------------<br />
        /// Importante: No añade a los estados posibles, Tendra que añadilos usted manualmente.<br />
        /// si quiero añadirlo automaticamente utilice 'CreateStateAutoAdd'.<br />
        /// </summary>
        /// <typeparam name="S">Estado que se quiera craer</typeparam>
        /// <returns>Es: Retorna el nuevo estado desactivado.</returns>
        public S CreateState<S>() where S : IState
        {
            return createState_internal<S>();
        }

        //public static T CreateState<T>(GameObject e_go, O eOwner, MachineState<O> e_ms) where T : IState
        //{
        //    T estado = e_go.AddComponent<T>();
        //    estado.enabled = false;
        //    //estado.Identificador = f_solicitarIde_i();
        //    estado.Owner = eOwner;
        //    estado.ConstructorGestion(e_ms);
        //    estado.Init(eOwner);

        //    return estado;
        //}

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Crea un nuevo estado de tipo T y lo inicializa con la dependencia proporcionada.<br />
        /// LLama al metodo Init() del estado.<br />
        /// -----------------<br />
        /// Importante: Añade automaticamente a los estados posibles.<br />
        /// </summary>
        /// <typeparam name="S">Estado que se quiera craer</typeparam>
        public void CreateStateAutoAdd<S>() where S : IState
        {
            S novoState = createState_internal<S>();

            novoState.Identificador = soliticeId_internal();

            if (!_possibleStates.Contains(novoState))
                _possibleStates.Add(novoState);
            else
                Debug.LogWarning($"({_host.GetType().Name}->MachineState): The State {novoState.GetType().Name} it already exists in the list of possible states.");
        }


        private S createState_internal<S>() where S : IState
        {
            S state;

#if UNITY_5_3_OR_NEWER
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(S)))
            {
                if (_host is GameObject go)
                {
                    var component = go.AddComponent(typeof(S));

                    if (component is S sComponent)
                    {
                        state = sComponent;
                    }
                    else
                    {
                        throw new InvalidCastException(
                            $"The component of type cannot be converted '{component.GetType().Name}' to '{typeof(S).Name}'."
                        );
                    }
                }
                else
                {
                    state = (S)Activator.CreateInstance(typeof(S));
                }

            }
            else
#endif
            {
                state = (S)Activator.CreateInstance(typeof(S));
            }

            state.enabled = false;
            state.Owner_internal = _owner;
            state.ManagerBuilder_internal(this);
            state.Init(_owner);
            state.Init();

            state.ChangeInt += (int valor) => ChangeState(valor);
            state.ChangeIState += (IState estado) => { CurrentState = estado; };

            state.Machine = this;

            return state;
        }

        // ***********************( Constructores )*********************** //
        public MachineState(object pHost, List<IState> pPossibleStates, O pOwner)
        {
            PossibleStates = pPossibleStates ?? new List<IState>();

            inicializar(pHost, pOwner);
        }
        public MachineState(object pHost, O pOwner)
        {
            inicializar(pHost, pOwner);
        }


        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        private void inicializar(object pHost, O pOwner)
        {
            if (pHost == null)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): El GameObject host es null en Inicializar.");
                return;
            }

            if (pOwner == null)
            {
                Debug.LogError($"({_host.GetType().Name}->MachineState): La fuente de datos es null en Inicializar.");
                return;
            }

            _transitions ??= new();
            _possibleStates ??= new(this);

            _host = pHost;
            _owner = pOwner;

            GestionadorMachineState.AddMachine_internal(this);
        }
    }

    // TODO: Que esto solo se encargue de gestionar las maquinas y añadir una funcion en la maquin para actualizar los update de los noMono.
    /// <summary>
    /// If you are not the MachinState developer, NEVER use anything in Spanish.
    /// </summary>
    public static class GestionadorMachineState 
    {
        // ***********************( Variables )*********************** //
        //public static GestionadorMachineState Instancia { get; private set; }
        private static List<IMachineState> _allMachines = new List<IMachineState>();
        private static int _crescendoId = 0;
        private static bool _init_flag = false;

        // ***********************( GSI )*********************** //

        // ***********************( Eventos )*********************** //

        // ***********************( Unity )*********************** //
        //private void Awake()
        //{
        //    if (Instancia == null)
        //    {
        //        Instancia = this;
        //        DontDestroyOnLoad(this);
        //    }
        //    else
        //    {
        //        Destroy(this);
        //    }

        //    if (_maquinasDeEstados == null)
        //        _maquinasDeEstados = new List<IMachineState>();
        //}

        //private void Update()
        //{
        //    this._maquinasDeEstados.ForEach(ms =>
        //    {
        //        if (ms.State != null && ms.State is IStateLittle noMono)
        //            noMono.Update();
        //    });
        //}

        //private void FixedUpdate()
        //{
        //    this._maquinasDeEstados.ForEach(ms =>
        //    {
        //        if (ms.Count >= 1)
        //            ms.UpdateTransitions();

        //        if (ms.State != null && ms.State is IStateLittle noMono)
        //            noMono.FixedUpdate();
        //    });
        //}


        // ***********************( Metodos )*********************** //
        private static void init()
        {
            if (_init_flag)
                return;
            _init_flag = true;

            updateTransition_internal();
        }
        private static async void updateTransition_internal()
        {
            while (true)
            {
                await Task.Delay(200);
                _allMachines.ForEach(ms =>
                {
                    if (ms.Count >= 1)
                        ms.UpdateTransitions();
                });
            }
        }



        private static int soliciteId_internal()
        {
            return ++_crescendoId;
        }
        public static void AddMachine_internal(IMachineState ms)
        {
            init();
            if (!_allMachines.Contains(ms))
            {
                ms.Identifier = soliciteId_internal();
                _allMachines.Add(ms);
            }
        }


    }
}