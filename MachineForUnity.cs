using System;
using System.Collections.Generic;
using System.Threading;
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
    all uppercase: is constant
    -- Functions & Class:
    starts: uppercase = public
    starts: lowercase = private
    starts: __ = public, Not recommended
    -- All:
    contain: _internal = divided not used final programer
    */




    public partial class MachineState<O> : IMachineState where O : class
    {
        public GameObject GetGameObject() => _host as GameObject;




        //TODO: To-Do
        // ***********************( Metodos Limpieza Estados )*********************** //
        private void destruirEstado(IState _estado)
        {
            if (_estado is IStateMonoBehaviour estado)
            {
                estado.DestroyThis();
            }
            else if (_estado is IStateLittle)
            {
                _estado = null; // rezemos para que funcione el garbage collector.
            }
        }

        private void limpiarUnEstado(IState eEstado)
        {
            if (eEstado != null)
            {
                if (eEstado == State)
                    State = null;

                eEstado.enabled = false;
                destruirEstado(eEstado);
            }
        }

        public void ClearImmediate(List<IState> _excluidosEspecificos = null)
        {
            List<int> _idesExluidos = new();
            if (_excluidosEspecificos != null)
            {
                foreach (var _estadoIndividual in _excluidosEspecificos)
                {
                    if (_estadoIndividual != null)
                        _idesExluidos.Add(_estadoIndividual.Identificador);
                }
            }

            foreach (var _estadoIndividual in _possibleStates)
            {
                if (_estadoIndividual != null)
                    _idesExluidos.Add(_estadoIndividual.Identificador);
            }

            IState[] _todosEstados = GetGameObject().GetComponents<IState>();
            foreach (var _estadoIndividual in _todosEstados)
            {
                if (
                    !_idesExluidos.Contains(_estadoIndividual.Identificador) &&
                    _estadoIndividual != null// &&
                                             //_estadoIndividual.MachineState == this
                    )
                {
                    limpiarUnEstado(_estadoIndividual);
                }
            }
        }
        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        /// <param name="_excluidosEspecificos"></param>
        public void Clear(List<IState> _excluidosEspecificos = null)
        {
            List<int> _idesExluidos = new();
            if (_excluidosEspecificos != null)
            {
                foreach (var _estadoIndividual in _excluidosEspecificos)
                {
                    if (_estadoIndividual != null)
                        _idesExluidos.Add(_estadoIndividual.Identificador);
                }
            }

            foreach (var _estadoIndividual in _possibleStates)
            {
                if (_estadoIndividual != null)
                    _idesExluidos.Add(_estadoIndividual.Identificador);
            }

            // TODO: Borrar todos los estados UNO por cada ciclo de Unity.
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void Rm(List<IState> _listaBorrar)
        {
            // TODO: Borrar una lista de estados.
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void Rm(IState _estadoBorrar)
        {
            // TODO: Borrar un estado.
        }



    }





















    public interface IStateMonoBehaviour
    {
        void DestroyThis();
    }





    public abstract class Base_StateBase : MonoBehaviour, IState, IStateMonoBehaviour
    {
        // ***********************( Variables/Declaraciones )*********************** //
        private object _owner { get; set; } = null;
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Clase padre/original donde se instancio la Maquina de Estados.<br />
        /// ___________________( English )___________________<br />
        /// Class parent/original where the State Machine was instantiated.<br />
        /// </summary>
        public object Owner
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

        object IState.Owner_internal
        {
            get => _owner;
            set => _owner = value;
        }

        private int _index = -1;

        private CancellationTokenSource _transicionToken;

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
        public O GetOwner<O>() where O : class
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
        public O GetOwnerForMachine<O>() where O : class
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
        private bool _first_flag = true;
        public bool InFirstEnter
        {
            get { return _first_flag; }
        }



        // ***********************( Eventos )*********************** //
        public event Action<int> OnChangeId;
        public event Action<int> ChangeInt;
        public event Action<IState> ChangeIState;


        // ***********************( Contructores )*********************** //
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        void IState.ManagerBuilder_internal<O>(MachineState<O> pMachine) 
        {
            _enterFrom = new Dictionary<Type, Action>();
            _exitFrom = new Dictionary<Type, Action>();

            StateHelper.RegisterDirectionality_internal(this, _enterFrom, _exitFrom);
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


        // ***********************( Metodos de Control )*********************** //
        void IState.ManageEnter_internal<O>(MachineState<O> pMachine)
        {
            SaveIndex(pMachine);
        }
        void IState.ManageAfterEnter_internal<O>(MachineState<O> pMachine)
        {
            StateHelper.CallFirstEnter_internal(this, ref _first_flag);
            StateHelper.StartTransition_internal(this, ref _transicionToken);
        }
        void IState.ManagerExit_internal<O>(MachineState<O> pMachine)
        {
            StateHelper.StopTransition_internal(ref _transicionToken);
        }
        void IState.ManagerAfterExit_internal<O>(MachineState<O> pMachine)
        {

        }
        void IState.OnEnteringPossibleStates_internal<O>(MachineState<O> pMachine)
        {
            SaveIndex(pMachine);
        }

        // ---> usuario: 
        public virtual void FirstEnter() { }
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
        /// Si entra al estado desde uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeEnter_internal<S>(S pState) => StateHelper.InvokeFor(_enterFrom, pState);

        /// <summary>
        /// si sale del estado hacia uno especificado anteriormente, se ejecutara la funcion asociada a ese estado.
        /// </summary>
        bool IState.ChangeExit_internal<S>(S pState) => StateHelper.InvokeFor(_exitFrom, pState);


        public int SaveIndex<O>(MachineState<O> pMachine) where O : class
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





}