using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sui.Machine
{
    // TODO: Implementar de forma explicita muchas funciones internas.
    public interface IState
    {
        // ***********************( Getter, Setters e Indesxadores )*********************** //
        MonoBehaviour Owner { get; set; }

        O GetOwner<O>() where O : MonoBehaviour => Owner as O;

        IMachineState Machine { get; set; }

        int Index { get; set; }
        Component ThisComponent { get; set; }
        bool Active { get; }

        // ***********************( Gestion y Control )*********************** //
        int Identificador { get; set; }
        int Id { get; }
        bool InFirstEnter { get; }

        bool enabled { get; set; }

        // ***********************( Eventos )*********************** //
        event Action OnFirtsEnter;
        event Action<int> OnChangeId;

        event Action<int> ChangeInt;
        event Action<IState> ChangeIState;

        // ***********************( Contructores )*********************** //
        void ConstructorGestion<O>(MachineState<O> maquina) where O : MonoBehaviour;
        void Init<T>(T owner);

        // ***********************( Metodos de Transiciones )*********************** //
        void EndTransition(int eProximo);
        void EndTrasition(IState eProximo);

        // ***********************( Metodos de Maquina )*********************** //
        IState ChangeState(int eEstado);
        IState ChangeState(IState eEstado);
        IState ChangeState<T>();

        int GetMyIndex();
        int GetIndex(string eName);
        int GetIndex(IState eEstado);
        int GetIndex<S>();

        IState GetState(int eIndex);
        string GetNameState(int eIndex);

        // ***********************( Control de direccion )*********************** //
        void EnterFrom<S>(Action _fun) where S : IState;
        void EnterFrom(Type _tipo, Action _fun);

        void AlEntrarEstadosPosibles<O>(MachineState<O> maquina) where O : MonoBehaviour;

        void Enter();
        void Exit();

        // ***********************( Metodos de Control )*********************** //
        void GestionEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour;
        void GestionTrasEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour;
        void GestionSalir<O>(MachineState<O> maquina) where O : MonoBehaviour;
        void GestionTrasSalir<O>(MachineState<O> maquina) where O : MonoBehaviour;

        // ***********************( Metodos Funcionales )*********************** //
        bool F_CambioEnter_b<S>(S eEstado) where S : IState;
        bool F_CambioExit_b<S>(S eEstado) where S : IState;
        int SaveIndex<O>(MachineState<O> maquina) where O : MonoBehaviour;
    }

    public interface IStateMonoBehaviour
    {
        void DestroyThis();
    }

    public interface IStateLittle
    {
        void Update();
        void FixedUpdate();
    }

    public interface ITransitionState
    {
        IEnumerator Transition();
    }

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
            set => _owner = value;
        }

        private int _indice_i = -1;
        private Component _esteComponente = null;

        private Coroutine _transicion;

        private Dictionary<Type, Action> _entrarDesde { get; set; } = new();
        private Dictionary<Type, Action> _salirDesde { get; set; } = new();

        IMachineState _maquina;

        // ***********************( Getter, Setters e Indesxadores )*********************** //
        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <returns></returns>
        public O GetOwner<O>() where O : MonoBehaviour => Owner as O;
        public int Index
        {
            get => _indice_i;
            set => _indice_i = value;
        }
        public Component ThisComponent
        {
            get => _esteComponente;
            set => _esteComponente = value;
        }
        public bool Active
        {
            get => enabled;
        }
        public bool enabled { get; set; } = true;

        public IMachineState Machine
        {
            get => _maquina;
            set => _maquina = value;
        }

        // ***********************( Gestion y Control )*********************** //
        // --- Gestion.
        private int _identificador_i = -1;
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public int Identificador
        {
            get
            {
                return _identificador_i;
            }
            set
            {
                _identificador_i = value;
                OnChangeId?.Invoke(_identificador_i);
            }
        }
        public int Id
        {
            get
            {
                return _identificador_i;
            }
        }

        // Obsoleto: Puedes llamar a Start() de Unity, Pero tu te fias? porque yo no.
        private bool _primeraVez_bandera = true;
        internal bool EntrarPrimeraVez
        {
            get
            {
                _primeraVez_bandera = false;
                return _primeraVez_bandera;
            }
            set
            {
                Debug.LogWarning($"(StateLittle->StateBase): 'InFirstEnter' -> {value}, Warning.");
                _primeraVez_bandera = value;
            }
        }
        public bool InFirstEnter
        {
            get { return _primeraVez_bandera; }
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
        public void ConstructorGestion<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            _entrarDesde = new Dictionary<Type, Action>();
            _salirDesde = new Dictionary<Type, Action>();

            //Debug.Log($"({gameObject.name}:StateBase): ConstructorGestion -> maquina:{maquina.GetType().FullName}.");

            // --- Atributos
            var _metodos = GetType().GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var _metodo in _metodos)
            {
                foreach (var _atributo in _metodo.GetCustomAttributes(true))
                {
                    if (_atributo is OnEnterFromAttribute _entrada)
                    {
                        Action _fun = (Action)Delegate.CreateDelegate(typeof(Action), this, _metodo);
                        EnterFrom(_entrada.Type, _fun);
                    }
                    else if (_atributo is OnExitToAttribute _salida)
                    {
                        Action _fun = (Action)Delegate.CreateDelegate(typeof(Action), this, _metodo);
                        ExitTo(_salida.Type, _fun);
                    }
                }
            }
        }
        public virtual void Init<O>(O owner) { }


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
        public void EnterFrom<S>(Action _fun) where S : IState
        {
            _entrarDesde[typeof(S)] = _fun;
        }
        public void EnterFrom(Type _tipo, Action _fun)
        {
            _entrarDesde[_tipo] = _fun;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado siguiente es igual al valor.
        /// <br />-----------------------<br />
        /// -Si el siguiente estado a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.
        /// <br />-----------------------
        /// </summary>
        public void ExitTo<S>(Action _fun) where S : IState
        {
            _salirDesde[typeof(S)] = _fun;
        }
        public void ExitTo(Type _tipo, Action _fun)
        {
            _salirDesde[_tipo] = _fun;
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
        public IState ChangeState(int eEstado)
        {
            return Machine.ChangeState(eEstado);
        }
        public IState ChangeState(IState eEstado)
        {
            return Machine.ChangeState(Machine.GetIndex(eEstado));
        }
        public IState ChangeState<T>()
        {
            return Machine.ChangeState<T>();
        }

        public int GetMyIndex()
        {
            return _indice_i;
        }
        public int GetIndex(string eName)
        {
            return Machine.GetIndex(eName);
        }
        public int GetIndex(IState eEstado)
        {
            return Machine.GetIndex(eEstado);
        }
        public int GetIndex<S>()
        {
            return Machine.GetIndex<S>();
        }

        public IState GetState(int eIndex)
        {
            return Machine.GetState(eIndex);
        }
        public string GetNameState(int eIndex)
        {
            return Machine.GetNameState(eIndex);
        }

        // ***********************( Metodos de Control )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            SaveIndex(maquina);
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionTrasEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            if (EntrarPrimeraVez)
                OnFirtsEnter?.Invoke();

            if (this is ITransitionState estado)
            {
                _transicion = maquina.StartCoroutine(estado.Transition());
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionSalir<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            if (_transicion != null)
            {
                maquina.StopCoroutine(ref _transicion);
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionTrasSalir<O>(MachineState<O> maquina) where O : MonoBehaviour
        {

        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void AlEntrarEstadosPosibles<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            SaveIndex(maquina);
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
        public bool F_CambioEnter_b<S>(S eEstado) where S : IState
        {
            if (eEstado == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_entrarDesde.Count() <= 0)
                return false;

            foreach (var item in _entrarDesde)
            {
                if (item.Key == eEstado.GetType())
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
        public bool F_CambioExit_b<S>(S eEstado) where S : IState
        {
            if (eEstado == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_salirDesde.Count() <= 0)
                return false;

            foreach (var item in _salirDesde)
            {
                if (item.Key == eEstado.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public int SaveIndex<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            _indice_i = maquina.GetIndex(this);
            return _indice_i;
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
            set => _owner = value;
        }

        private int _indice_i = -1;
        private Component _esteComponente = null;

        private Coroutine _transicion;

        private Dictionary<Type, Action> _entrarDesde { get; set; } = new();
        private Dictionary<Type, Action> _salirDesde { get; set; } = new();

        IMachineState _maquina;

        // ***********************( Getter, Setters e Indesxadores )*********************** //
        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        /// <typeparam name="O"></typeparam>
        /// <returns></returns>
        public O GetOwner<O>() where O : MonoBehaviour => Owner as O;
        public int Index
        {
            get => _indice_i;
            set => _indice_i = value;
        }
        public Component ThisComponent
        {
            get => _esteComponente;
            set => _esteComponente = value;
        }
        public bool Active
        {
            get => enabled;
        }

        public IMachineState Machine
        {
            get => _maquina;
            set => _maquina = value;
        }

        // ***********************( Gestion y Control )*********************** //
        // --- Gestion.
        private int _identificador_i = -1;
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public int Identificador
        {
            get
            {
                return _identificador_i;
            }
            set
            {
                _identificador_i = value;
                OnChangeId?.Invoke(_identificador_i);
            }
        }
        public int Id
        {
            get
            {
                return _identificador_i;
            }
        }

        // Obsoleto: Puedes llamar a Start() de Unity, Pero tu te fias? porque yo no.
        private bool _primeraVez_bandera = true;
        internal bool EntrarPrimeraVez
        {
            get
            {
                _primeraVez_bandera = false;
                return _primeraVez_bandera;
            }
            set
            {
                Debug.LogWarning($"({gameObject.name}:StateBase): 'InFirstEnter' -> {value}, Warning.");
                _primeraVez_bandera = value;
            }
        }
        public bool InFirstEnter
        {
            get { return _primeraVez_bandera; }
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
        public void ConstructorGestion<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            // Deberia funcionar pero hay que testealo pues tengo malas experiencias.
            this.ThisComponent = this.GetComponent(this.GetType());

            _entrarDesde = new Dictionary<Type, Action>();
            _salirDesde = new Dictionary<Type, Action>();

            //Debug.Log($"({gameObject.name}:StateBase): ConstructorGestion -> maquina:{maquina.GetType().FullName}.");

            // --- Atributos
            var _metodos = GetType().GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            foreach (var _metodo in _metodos)
            {
                foreach (var _atributo in _metodo.GetCustomAttributes(true))
                {
                    if (_atributo is OnEnterFromAttribute _entrada)
                    {
                        Action _fun = (Action)Delegate.CreateDelegate(typeof(Action), this, _metodo);
                        EnterFrom(_entrada.Type, _fun);
                    }
                    else if (_atributo is OnExitToAttribute _salida)
                    {
                        Action _fun = (Action)Delegate.CreateDelegate(typeof(Action), this, _metodo);
                        ExitTo(_salida.Type, _fun);
                    }
                }
            }
        }
        public virtual void Init<O>(O owner) { }


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
        public void EnterFrom<S>(Action _fun) where S : IState
        {
            _entrarDesde[typeof(S)] = _fun;
        }
        public void EnterFrom(Type _tipo, Action _fun)
        {
            _entrarDesde[_tipo] = _fun;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado siguiente es igual al valor.
        /// <br />-----------------------<br />
        /// -Si el siguiente estado a este es S.<br />
        /// -La funcion con la clabe a S se ejecutara.
        /// <br />-----------------------
        /// </summary>
        public void ExitTo<S>(Action _fun) where S : IState
        {
            _salirDesde[typeof(S)] = _fun;
        }
        public void ExitTo(Type _tipo, Action _fun)
        {
            _salirDesde[_tipo] = _fun;
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
        public IState ChangeState(int eEstado)
        {
            return Machine.ChangeState(eEstado);
        }
        public IState ChangeState(IState eEstado)
        {
            return Machine.ChangeState(Machine.GetIndex(eEstado));
        }
        public IState ChangeState<T>()
        {
            return Machine.ChangeState<T>();
        }

        public int GetMyIndex()
        {
            return _indice_i;
        }
        public int GetIndex(string eName)
        {
            return Machine.GetIndex(eName);
        }
        public int GetIndex(IState eEstado)
        {
            return Machine.GetIndex(eEstado);
        }
        public int GetIndex<S>()
        {
            return Machine.GetIndex<S>();
        }

        public IState GetState(int eIndex)
        {
            return Machine.GetState(eIndex);
        }
        public string GetNameState(int eIndex)
        {
            return Machine.GetNameState(eIndex);
        }

        // ***********************( Metodos de Control )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            ThisComponent = GetComponent(GetType());
            SaveIndex(maquina);
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionTrasEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            if (EntrarPrimeraVez)
                OnFirtsEnter?.Invoke();

            if (this is ITransitionState estado)
            {
                _transicion = StartCoroutine(estado.Transition());
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionSalir<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            if (_transicion != null)
            {
                maquina.StopCoroutine(ref _transicion);
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void GestionTrasSalir<O>(MachineState<O> maquina) where O : MonoBehaviour
        {

        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void AlEntrarEstadosPosibles<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            SaveIndex(maquina);
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
        public bool F_CambioEnter_b<S>(S eEstado) where S : IState
        {
            if (eEstado == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_entrarDesde.Count() <= 0)
                return false;

            foreach (var item in _entrarDesde)
            {
                if (item.Key == eEstado.GetType())
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
        public bool F_CambioExit_b<S>(S eEstado) where S : IState
        {
            if (eEstado == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_salirDesde.Count() <= 0)
                return false;

            foreach (var item in _salirDesde)
            {
                if (item.Key == eEstado.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }

        public int SaveIndex<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            _indice_i = maquina.GetIndex(this);
            return _indice_i;
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