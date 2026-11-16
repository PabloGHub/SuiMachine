using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    -- Functions & Class:
    starts: uppercase = public
    starts: lowercase = private
    starts: __ = public, Not recommended
    -- All:
    contain: _internal = divided not used final programer
    */



    // Puto Unity.
    public abstract class Intemediario_Little : IStateLittle
    {
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
    }

    // TODO: Implementar un sistema para estados pequeños que no necesiten MonoBehaviour.
    public abstract class Base_StateBase_Little : Intemediario_Little, IState
    {
        // ***********************( Variables/Declaraciones )*********************** //
        private MonoBehaviour _owner { get; set; } = null;
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Clase padre/original donde se instancio la Maquina de Estados.<br />
        /// ___________________( English )___________________<br />
        /// Class parent/original where the State Machine was instantiated.<br />
        /// </summary>
        public MonoBehaviour Owner
        {
            get
            {
                if (_owner == null)
                {
                    Debug.LogError($"(StateLittle->StateBase): 'Owner' is null, Please use it from Init.");
                }
                return _owner;
            }
        }

        MonoBehaviour IState.Owner_internal
        {
            get => _owner;
            set => _owner = value;
        }

        private int _index = -1;

        private Coroutine _transicion;

        private Dictionary<Type, Action> _enterFrom { get; set; } = new();
        private Dictionary<Type, Action> _exitFrom { get; set; } = new();

        IMachineState _machine;

        private ChangeStateOptions _opt = ChangeStateOptions.None;

        // ***********************( Getter, Setters e Indesxadores )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Devuelve el dueño de la maquina de estados casteado al tipo especificado.<br />
        /// ___________________( English )___________________<br />
        /// Returns the owner of the state machine cast to the specified type.<br />
        /// </summary>
        /// <typeparam name="O">ES: TipoDato del dueño <br />EN: DataType of the owner</typeparam>
        /// <returns>Owner but "O" type</returns>
        public O GetOwner<O>() where O : MonoBehaviour
        {
            return Owner as O;
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Devuelve el dueño de la maquina de estados casteado al tipo especificado.<br />
        /// Lo coge directamente de la maquina de estados.<br />
        /// ___________________( English )___________________<br />
        /// Returns the owner of the state machine cast to the specified type.<br />
        /// It takes it directly from the state machine.<br />
        /// </summary>
        /// <typeparam name="O">ES: TipoDato del dueño <br />EN: DataType of the owner</typeparam>
        /// <returns>Owner but "O" type</returns>
        public O GetOwnerForMachine<O>() where O : MonoBehaviour
        {
            return _machine.GetOwner<O>();
        }

        public int Index
        {
            get => _index;
            set => _index = value;
        }
        public bool Active
        {
            get => enabled;
        }
        public bool enabled { get; set; } = true;

        public IMachineState Machine
        {
            get => _machine;
            set => _machine = value;
        }

        public ChangeStateOptions ChangeOptions
        {
            get => _opt;
            set => _opt = value;
        }

        // ***********************( Gestion y Control )*********************** //
        // --- Gestion.
        private int _identifier = -1;
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        int IState.Identificador
        {
            get
            {
                return _identifier;
            }
            set
            {
                _identifier = value;
                OnChangeId?.Invoke(_identifier);
            }
        }
        public int Id
        {
            get
            {
                return _identifier;
            }
        }


        private bool _first_flag = true;
        internal bool FirstOne_internal
        {
            get
            {
                _first_flag = false;
                return _first_flag;
            }
            set
            {
                Debug.LogWarning($"(StateLittle->StateBase): 'InFirstEnter' -> {value}, Warning.");
                _first_flag = value;
            }
        }
        public bool InFirstEnter
        {
            get { return _first_flag; }
        }


        // ***********************( Eventos )*********************** //
        public event Action OnFirtsEnter;
        public event Action<int> OnChangeId;
        public event Action<int> ChangeInt;
        public event Action<IState> ChangeIState;


        // ***********************( Contructores )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerBuilder_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            _enterFrom = new Dictionary<Type, Action>();
            _exitFrom = new Dictionary<Type, Action>();

            //Debug.Log($"({gameObject.name}:StateBase): ConstructorGestion -> maquina:{maquina.GetType().FullName}.");

            // --- Atributos
            var methods = GetType().GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                foreach (var atr in method.GetCustomAttributes(true))
                {
                    if (atr is OnEnterFromAttribute enter)
                    {
                        Action fun = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                        EnterFrom(enter.Type, fun);
                    }
                    else if (atr is OnExitToAttribute exit)
                    {
                        Action fun = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                        ExitTo(exit.Type, fun);
                    }
                }
            }
        }
        public virtual void Init<O>(O pOwner) { }
        public virtual void Init() { }

        // ***********************( Control de direccion )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado anterior es igual al valor.<br />
        /// -Si el estado anterior a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.<br />
        /// -----------------------<br />
        /// Nota: Solo puedes tener una funcion por estado entrante.<br />
        /// ___________________( English )___________________<br />
        /// Only the function will be called when the previous state is equal to the value.<br />
        /// -If the previous state to this is S.<br />
        /// -The function with the key to S will be executed.<br />
        /// -----------------------<br />
        /// Note: You can only have one function per enter state.
        /// </summary>
        public void EnterFrom<S>(Action pFun) where S : IState
        {
            _enterFrom[typeof(S)] = pFun;
        }
        public void EnterFrom(Type pType, Action pFun)
        {
            _enterFrom[pType] = pFun;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado siguiente es igual al valor.
        /// <br />-----------------------<br />
        /// -Si el siguiente estado a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.
        /// <br />-----------------------
        /// </summary>
        public void ExitTo<S>(Action pFun) where S : IState
        {
            _exitFrom[typeof(S)] = pFun;
        }
        public void ExitTo(Type pType, Action pFun)
        {
            _exitFrom[pType] = pFun;
        }

        // ***********************( Metodos de Transiciones )*********************** //
        public void EndTransition(int eProximo)
        {
            ChangeInt?.Invoke(eProximo);
        }
        public void EndTrasition(IState eProximo)
        {
            ChangeIState?.Invoke(eProximo);
        }

        // ***********************( Metodos de Maquina )*********************** //
        public (IState newS, IState oldS) ChangeState(int pState)
        {
            return Machine.ChangeState(pState);
        }
        public (IState newS, IState oldS) ChangeState(IState pState)
        {
            return Machine.ChangeState(Machine.GetIndex(pState));
        }
        public (IState newS, IState oldS) ChangeState<T>()
        {
            return Machine.ChangeState<T>();
        }

        public int GetMyIndex()
        {
            return _index;
        }
        public int GetIndex(string pName)
        {
            return Machine.GetIndex(pName);
        }
        public int GetIndex(IState pState)
        {
            return Machine.GetIndex(pState);
        }
        public int GetIndex<S>()
        {
            return Machine.GetIndex<S>();
        }

        public IState GetState(int pIndex)
        {
            return Machine.GetState(pIndex);
        }
        public string GetNameState(int pIndex)
        {
            return Machine.GetNameState(pIndex);
        }

        // ***********************( Metodos de Control )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManageEnter_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            SaveIndex(pMachine);
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManageAfterEnter_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            if (FirstOne_internal)
                OnFirtsEnter?.Invoke();

            if (this is ITransitionState pState)
            {
                _transicion = pMachine.StartCoroutine(pState.Transition());
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerExit_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            if (_transicion != null)
            {
                pMachine.StopCoroutine(ref _transicion);
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerAfterExit_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {

        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.OnEnteringPossibleStates_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            SaveIndex(pMachine);
        }

        // ---> usuario: 
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Se ejecutara al entrar al estado.<br />
        /// - Al llamar a Start() se ejecutara la primera vez que entre al estado.<br />
        /// - Se llama despues de OnEnlable()<br />
        /// ___________________( English )___________________<br />
        /// Will be executed when entering the state.<br />
        /// - When calling Start(), it will be executed the first time you enter the state.<br />
        /// - It is called after OnEnable().<br />
        /// </summary>
        public abstract void Enter();
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Se ejecutara al salir del estado.<br />
        /// - Se llama antes de OnDisable().<br />
        /// ___________________( English )___________________<br />
        /// Will be executed when leaving the state.<br />
        /// - It is called before OnDisable().<br />
        /// </summary>
        public abstract void Exit();

        /*
        /// <summary>
        /// En_proceso.
        /// </summary>
        /// <returns></returns>
        public virtual Task EnterAsync()
        {
            Enter();
            return Task.CompletedTask;
        }
        /// <summary>
        /// En_proceso.
        /// </summary>
        /// <returns></returns>
        public virtual Task ExitAsync()
        {
            Exit();
            return Task.CompletedTask;
        }
        */

        // ***********************( Mi Unity )*********************** //
        // ***********************( Unity -> Mi )*********************** //

        // ***********************( Metodos Funcionales )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.<br /><br />
        /// Si entra al estado desde uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeEnter_internal<S>(S pState) // where S : IState
        {
            if (pState == null)
            {
                // Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_enterFrom.Count() <= 0)
                return false;

            foreach (var item in _enterFrom)
            {
                if (item.Key == pState.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.<br /><br />
        /// si sale del estado hacia uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeExit_internal<S>(S pState) // where S : IState
        {
            if (pState == null)
            {
                // Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_exitFrom.Count() <= 0)
                return false;

            foreach (var item in _exitFrom)
            {
                if (item.Key == pState.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public int SaveIndex<O>(MachineState<O> pMachine) where O : MonoBehaviour
        {
            _index = pMachine.GetIndex(this);
            return _index;
        }
    }

    public abstract class Base_StateBase : MonoBehaviour, IState, IStateMonoBehaviour
    {
        // ***********************( Variables/Declaraciones )*********************** //
        private MonoBehaviour _owner { get; set; } = null;
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Clase padre/original donde se instancio la Maquina de Estados.<br />
        /// ___________________( English )___________________<br />
        /// Class parent/original where the State Machine was instantiated.<br />
        /// </summary>
        public MonoBehaviour Owner
        {
            get
            {
                if (_owner == null)
                {
                    Debug.LogError($"({gameObject.name}:StateBase): 'Owner' is null, Please use it from Init.");
                }
                return _owner;
            }
        }

        MonoBehaviour IState.Owner_internal
        {
            get => _owner;
            set => _owner = value;
        }

        private int _index = -1;

        private Coroutine _transicion;

        private Dictionary<Type, Action> _enterFrom { get; set; } = new();
        private Dictionary<Type, Action> _exitFrom { get; set; } = new();

        IMachineState _machine;

        private ChangeStateOptions _opt = ChangeStateOptions.None;

        // ***********************( Getter, Setters e Indesxadores )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Devuelve el dueño de la maquina de estados casteado al tipo especificado.<br />
        /// ___________________( English )___________________<br />
        /// Returns the owner of the state machine cast to the specified type.<br />
        /// </summary>
        /// <typeparam name="O">ES: TipoDato del dueño <br />EN: DataType of the owner</typeparam>
        /// <returns>Owner but "O" type</returns>
        public O GetOwner<O>() where O : MonoBehaviour
        {
            return Owner as O;
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Devuelve el dueño de la maquina de estados casteado al tipo especificado.<br />
        /// Lo coge directamente de la maquina de estados.<br />
        /// ___________________( English )___________________<br />
        /// Returns the owner of the state machine cast to the specified type.<br />
        /// It takes it directly from the state machine.<br />
        /// </summary>
        /// <typeparam name="O">ES: TipoDato del dueño <br />EN: DataType of the owner</typeparam>
        /// <returns>Owner but "O" type</returns>
        public O GetOwnerForMachine<O>() where O : MonoBehaviour
        {
            return _machine.GetOwner<O>();
        }

        public int Index
        {
            get => _index;
            set => _index = value;
        }
        public bool Active
        {
            get => enabled;
        }

        public IMachineState Machine
        {
            get => _machine;
            set => _machine = value;
        }

        public ChangeStateOptions ChangeOptions
        {
            get => _opt;
            set => _opt = value;
        }

        // ***********************( Gestion y Control )*********************** //
        // --- Gestion.
        private int _identifier = -1;
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        int IState.Identificador
        {
            get
            {
                return _identifier;
            }
            set
            {
                _identifier = value;
                OnChangeId?.Invoke(_identifier);
            }
        }
        public int Id
        {
            get
            {
                return _identifier;
            }
        }

        // Obsoleto: Puedes llamar a Start() de Unity, Pero tu te fias? porque yo no.
        private bool _frist_flag = true;
        internal bool FirstOne_internal
        {
            get
            {
                _frist_flag = false;
                return _frist_flag;
            }
            set
            {
                Debug.LogWarning($"({gameObject.name}:StateBase): 'InFirstEnter' -> {value}, Warning.");
                _frist_flag = value;
            }
        }
        public bool InFirstEnter
        {
            get { return _frist_flag; }
        }



        // ***********************( Eventos )*********************** //
        public event Action OnFirtsEnter;
        public event Action<int> OnChangeId;
        public event Action<int> ChangeInt;
        public event Action<IState> ChangeIState;


        // ***********************( Contructores )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerBuilder_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            _enterFrom = new Dictionary<Type, Action>();
            _exitFrom = new Dictionary<Type, Action>();

            //Debug.Log($"({gameObject.name}:StateBase): ConstructorGestion -> maquina:{maquina.GetType().FullName}.");

            // --- Atributos
            var methods = GetType().GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                foreach (var atr in method.GetCustomAttributes(true))
                {
                    if (atr is OnEnterFromAttribute enter)
                    {
                        Action fun = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                        EnterFrom(enter.Type, fun);
                    }
                    else if (atr is OnExitToAttribute exit)
                    {
                        Action fun = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                        ExitTo(exit.Type, fun);
                    }
                }
            }
        }
        public virtual void Init<O>(O pOwner) { }
        public virtual void Init() { }

        // ***********************( Control de direccion )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado anterior es igual al valor.<br />
        /// -Si el estado anterior a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.<br />
        /// -----------------------<br />
        /// Nota: Solo puedes tener una funcion por estado entrante.<br />
        /// ___________________( English )___________________<br />
        /// Only the function will be called when the previous state is equal to the value.<br />
        /// -If the previous state to this is S.<br />
        /// -The function with the key to S will be executed.<br />
        /// -----------------------<br />
        /// Note: You can only have one function per enter state.
        /// </summary>
        public void EnterFrom<S>(Action pFun) where S : IState
        {
            _enterFrom[typeof(S)] = pFun;
        }
        public void EnterFrom(Type pType, Action pFun)
        {
            _enterFrom[pType] = pFun;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado siguiente es igual al valor.
        /// <br />-----------------------<br />
        /// -Si el siguiente estado a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.
        /// <br />-----------------------
        /// </summary>
        public void ExitTo<S>(Action pFun) where S : IState
        {
            _exitFrom[typeof(S)] = pFun;
        }
        public void ExitTo(Type pType, Action pFun)
        {
            _exitFrom[pType] = pFun;
        }

        // ***********************( Metodos de Transiciones )*********************** //
        public void EndTransition(int pNext)
        {
            ChangeInt?.Invoke(pNext);
        }
        public void EndTrasition(IState pNext)
        {
            ChangeIState?.Invoke(pNext);
        }



        // ***********************( Metodos de Maquina )*********************** //
        public (IState newS, IState oldS) ChangeState(int pState)
        {
            return Machine.ChangeState(pState);
        }
        public (IState newS, IState oldS) ChangeState(IState pState)
        {
            return Machine.ChangeState(Machine.GetIndex(pState));
        }
        public (IState newS, IState oldS) ChangeState<T>()
        {
            return Machine.ChangeState<T>();
        }

        public int GetMyIndex()
        {
            return _index;
        }
        public int GetIndex(string pName)
        {
            return Machine.GetIndex(pName);
        }
        public int GetIndex(IState pState)
        {
            return Machine.GetIndex(pState);
        }
        public int GetIndex<S>()
        {
            return Machine.GetIndex<S>();
        }

        public IState GetState(int pIndex)
        {
            return Machine.GetState(pIndex);
        }
        public string GetNameState(int pIndex)
        {
            return Machine.GetNameState(pIndex);
        }

        // ***********************( Metodos de Control )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManageEnter_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            SaveIndex(pMachine);
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManageAfterEnter_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            if (FirstOne_internal)
                OnFirtsEnter?.Invoke();

            if (this is ITransitionState pState)
            {
                _transicion = StartCoroutine(pState.Transition());
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerExit_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            if (_transicion != null)
            {
                pMachine.StopCoroutine(ref _transicion);
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerAfterExit_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {

        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.OnEnteringPossibleStates_internal<O>(MachineState<O> pMachine) // where O : MonoBehaviour
        {
            SaveIndex(pMachine);
        }

        // ---> usuario: 
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Se ejecutara al entrar al estado.<br />
        /// - Al llamar a Start() se ejecutara la primera vez que entre al estado.<br />
        /// - Se llama despues de OnEnlable()<br />
        /// ___________________( English )___________________<br />
        /// Will be executed when entering the state.<br />
        /// - When calling Start(), it will be executed the first time you enter the state.<br />
        /// - It is called after OnEnable().<br />
        /// </summary>
        public abstract void Enter();
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Se ejecutara al salir del estado.<br />
        /// - Se llama antes de OnDisable().<br />
        /// ___________________( English )___________________<br />
        /// Will be executed when leaving the state.<br />
        /// - It is called before OnDisable().<br />
        /// </summary>
        public abstract void Exit();

        /*
        /// <summary>
        /// En_proceso.
        /// </summary>
        /// <returns></returns>
        public virtual Task EnterAsync()
        {
            Enter();
            return Task.CompletedTask;
        }
        /// <summary>
        /// En_proceso.
        /// </summary>
        /// <returns></returns>
        public virtual Task ExitAsync()
        {
            Exit();
            return Task.CompletedTask;
        }
        */

        // ***********************( Mi Unity )*********************** //
        // ***********************( Unity -> Mi )*********************** //

        // ***********************( Metodos Funcionales )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.<br /><br />
        /// Si entra al estado desde uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeEnter_internal<S>(S pMachine) // where S : IState
        {
            if (pMachine == null)
            {
                // Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_enterFrom.Count() <= 0)
                return false;

            foreach (var item in _enterFrom)
            {
                if (item.Key == pMachine.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.<br /><br />
        /// si sale del estado hacia uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeExit_internal<S>(S pState) //  where S : IState
        {
            if (pState == null)
            {
                // Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_exitFrom.Count() <= 0)
                return false;

            foreach (var item in _exitFrom)
            {
                if (item.Key == pState.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public int SaveIndex<O>(MachineState<O> pMachine) where O : MonoBehaviour
        {
            _index = pMachine.GetIndex(this);
            return _index;
        }

        // ***********************( Metodos Gestion )*********************** //
        /// <summary>
        /// Try, to see what he does. :)
        /// </summary>
        public void DestroyThis()
        {
            Destroy(this);
        }
    }

    public abstract class StateBase : Base_StateBase
    {
    }

    public abstract class LittleStateBase : Base_StateBase_Little
    {
    }

    // ***********************( Atributos )*********************** //
    // En cuanto Unity Admita C# 11 Pasar a valores genericos.
    // TODO: Recordar como se hacia eso, recuerdo que era para evitar el uso de typeof.

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OnEnterFromAttribute : Attribute
    {
        public Type Type { get; }
        public OnEnterFromAttribute(Type type)
        {
            Type = type;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OnExitToAttribute : Attribute
    {
        public Type Type { get; }
        public OnExitToAttribute(Type type)
        {
            Type = type;
        }
    }

}

/* // EJEMPLO DE COPILOT //
 public abstract class EstadoBase
{
    public abstract void Entrar();
    public abstract void Actualizar();
    public abstract void Salir();
}

public class EstadoCaminar : EstadoBase
{
    public override void Entrar() { Debug.Log("Entrando en estado Caminar"); }
    public override void Actualizar() { Debug.Log("Actualizando estado Caminar"); }
    public override void Salir() { Debug.Log("Saliendo de estado Caminar"); }
}

public class ControladorNazareno : MonoBehaviour
{
    private EstadoBase estadoActual;

    public void CambiarEstado(EstadoBase nuevoEstado)
    {
        if (estadoActual != null)
            estadoActual.Salir();

        estadoActual = nuevoEstado;
        estadoActual.Entrar();
    }

    private void Update()
    {
        if (estadoActual != null)
            estadoActual.Actualizar();
    }
}
 */