using System.Collections.Generic;
using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

namespace Sui.Machine
{
    public abstract class StateBase : MonoBehaviour
    {
        // ***********************( Variables/Declaraciones )*********************** //
        private MonoBehaviour _source { get; set; } = null;
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Clase padre/original donde se instancio la Maquina de Estados.<br />
        /// ___________________( English )_________________<br />
        /// Class parent/original where the State Machine was instantiated.<br />
        /// </summary>
        public MonoBehaviour Source
        {
            get
            {
                if (_source == null)
                {
                    Debug.LogError($"({gameObject.name}:StateBase): 'Source' is null, Please use it from Init.");
                }
                return _source;
            }
            set => _source = value;
        }

        private int _indice_i = -1;
        private Component _esteComponente = null;

        private Dictionary<Type, Action> _entrarDesde { get; set; } = new();
        private Dictionary<Type, Action> _salirDesde { get; set; } = new();

        // ***********************( Getter y Setters )*********************** //
        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetSource<T>() where T : MonoBehaviour => Source as T;
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

        // ***********************( Gestion y Control )*********************** //
        // --- Gestion.
        private int _identificador_i = -1;
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal int Identificador
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

        // Obsoleto: Puedes llamar a Start() de Unity.
        //private bool _primeraVez_bandera = true;
        //internal bool EntrarPrimeraVez
        //{
        //    get
        //    {
        //        _primeraVez_bandera = false;
        //        return _primeraVez_bandera;
        //    }
        //    set
        //    {
        //        Debug.LogWarning($"({gameObject.name}:StateBase): 'InFirstEnter' -> {value}, Warning.");
        //        _primeraVez_bandera = value;
        //    }
        //}
        //public bool InFirstEnter
        //{
        //    get { return _primeraVez_bandera; }
        //}



        // ***********************( Eventos )*********************** //
        //public event Action OnFirtsEnter;
        public event Action<int> OnChangeId;


        // ***********************( Contructores )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void ConstructorGestion<O>(MachineState<O> maquina) where O : MonoBehaviour
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
                        OnEnterFrom(_entrada.Type, _fun);
                    }
                    else if (_atributo is OnExitToAttribute _salida)
                    {
                        Action _fun = (Action)Delegate.CreateDelegate(typeof(Action), this, _metodo);
                        OnExitTo(_salida.Type, _fun);
                    }
                }
            }
        }
        public virtual void Init<T>(T source) { }


        // ***********************( Control de direccion )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado anterior es igual al valor.<br />
        /// -Si el estado anterior a este es T.<br />
        /// -La funcion con la clabe a T se ejecutara.<br />
        /// -----------------------<br />
        /// Nota: Solo puedes tener una funcion por estado.<br />
        /// ___________________( English )___________________<br />
        /// Only the function will be called when the previous state is equal to the value.<br />
        /// -If the previous state to this is T.<br />
        /// -The function with the key to T will be executed.<br />
        /// -----------------------<br />
        /// Note: You can only have one function per state.
        /// </summary>
        public void OnEnterFrom<T>(Action _fun) where T : StateBase
        {
            _entrarDesde[typeof(T)] = _fun;
        }
        public void OnEnterFrom(Type _tipo, Action _fun)
        {
            _entrarDesde[_tipo] = _fun;
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Solo se llamara a la funcion cuando el estado siguiente es igual al valor.
        /// <br />-----------------------<br />
        /// -Si el siguiente estado a este es T.<br />
        /// -La funcion con la clabe a T se ejecutara.
        /// <br />-----------------------
        /// </summary>
        public void OnExitTo<T>(Action _fun) where T : StateBase
        {
            _salirDesde[typeof(T)] = _fun;
        }
        public void OnExitTo(Type _tipo, Action _fun)
        {
            _salirDesde[_tipo] = _fun;
        }


        // ***********************( Metodos de Control )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void GestionEntrar<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            ThisComponent = GetComponent(GetType());
            GetIndex(maquina);

            StateBase _estado = Transition();
            if (_estado != null)
            {
                maquina.ChangeState(maquina[_estado]);
            }
            else
            {
                int _indice_i = TransitionIndex();
                if (_indice_i >= 0)
                {
                    maquina.ChangeState(_indice_i);
                }
            }
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void GestionTrasEntrar()
        {
            //if (EntrarPrimeraVez)
            //    OnFirtsEnter?.Invoke();
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void GestionSalir()
        {

        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void GestionTrasSalir()
        {
            
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        internal void AlEntrarEstadosPosibles<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            GetIndex(maquina);
        }

        // ---> usuario: 
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Se ejecutara al entrar al estado.<br />
        /// - Al llamar a Start() se ejecutara la primera vez que entre al estado.<br />
        /// - Se llama despues de OnEnlable()<br />
        /// _____________________( English )___________________<br />
        /// Will be executed when entering the state.<br />
        /// - When calling Start(), it will be executed the first time you enter the state.<br />
        /// - It is called after OnEnable().<br />
        /// </summary>
        public abstract void Enter();
        /// <summary>
        /// ____________________( Español )___________________<br />
        /// Se ejecutara al salir del estado.<br />
        /// - Se llama antes de OnDisable().<br />
        /// _____________________( English )___________________<br />
        /// Will be executed when leaving the state.<br />
        /// - It is called before OnDisable().<br />
        /// </summary>
        public abstract void Exit();

        public virtual StateBase Transition()
        {
            return null;
        }
        public virtual int TransitionIndex()
        {
            return -1;
        }


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


        // ***********************( Mi Unity )*********************** //
        //public virtual void MiAwake() { }
        //public virtual void MiOnEnable() { }
        //public virtual void MiStart() { }
        //public virtual void MiFixedUpdate() { }
        //public virtual void MiUpdate() { }
        //public virtual void MiLateUpdate() { }
        //public virtual void MiOnDisable() { }
        //public virtual void MiOnDestroy() { }





        // ***********************( Unity -> Mi )*********************** //
        //private void Awake()
        //{
        //    MiAwake();
        //}
        //private void OnEnable()
        //{
        //    MiOnEnable();
        //}
        //private void Start()
        //{
        //    MiStart();
        //}
        //private void FixedUpdate()
        //{
        //    MiFixedUpdate();
        //}
        //private void Update()
        //{
        //    MiUpdate();
        //}
        //private void LateUpdate()
        //{
        //    MiLateUpdate();
        //}
        //private void OnDisable()
        //{
        //    MiOnDisable();
        //}
        //private void OnDestroy()
        //{
        //    MiOnDestroy();
        //}


        // ***********************( Metodos Funcionales )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.<br /><br />
        /// Si entra al estado desde uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        internal bool f_CambioEnter_b<T>(T _estado_T)
        {
            if (_estado_T == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_entrarDesde.Count() <= 0)
                return false;

            foreach (var item in _entrarDesde)
            {
                if (item.Key.GetType() == _estado_T.GetType())
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
        internal bool f_CambioExit_b<T>(T _estado_T)
        {
            if (_estado_T == null)
            {
                Debug.LogError($"(StateBase): El estado pasado es nulo.");
                return false;
            }

            if (_salirDesde.Count() <= 0)
                return false;

            foreach (var item in _salirDesde)
            {
                if (item.Key.GetType() == _estado_T.GetType())
                {
                    item.Value?.Invoke();
                    return true;
                }
            }

            return false;
        }

        int GetIndex<O>(MachineState<O> maquina) where O : MonoBehaviour
        {
            int _indice_i = maquina.GetIndex(this);
            Index = _indice_i;
            return _indice_i;
        }

        // ***********************( Metodos Gestion )*********************** //
        /// <summary>
        /// Try, to see what he does. :)
        /// </summary>
        internal void destroyThis()
        {
            Destroy(this);
        }
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