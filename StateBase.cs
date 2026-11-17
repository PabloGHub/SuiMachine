using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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




    // TODO: Implementar de forma explicita muchas funciones internas.
    public interface IState
    {
        // ***********************( Getter, Setters e Indesxadores )*********************** //
        object Owner { get; }
        object Owner_internal { get; set; }

        O GetOwner<O>() where O : class => Owner as O;

        IMachineState Machine { get; set; }

        int Index { get; set; }
        bool Active { get; }

        ChangeStateOptions ChangeOptions { get; set; }

        // ***********************( Gestion y Control )*********************** //
        int Identificador { get; set; }
        int Id { get; }
        bool InFirstEnter { get; }

        bool enabled { get; set; }

        // ***********************( Eventos )*********************** //
        //event Action OnFirtsEnter;
        [Obsolete]
        event Action<int> OnChangeId;
        [Obsolete]
        event Action<int> ChangeInt;
        event Action<IState> ChangeIState;

        // ***********************( Contructores )*********************** //
        void ManagerBuilder_internal<O>(MachineState<O> pMachine) where O : class;
        void Init<O>(O owner);
        void Init();

        // ***********************( Metodos de Transiciones )*********************** //
        //void EndTransition(int pNext);
        //void EndTrasition(IState pNext);

        // ***********************( Metodos de Maquina )*********************** //
        //(IState newS, IState oldS) ChangeState(int pStateIndex);
        //(IState newS, IState oldS) ChangeState(IState pState);
        //(IState newS, IState oldS) ChangeState<T>();


        //int GetMyIndex();
        //int GetIndex(string eName);
        //int GetIndex(IState pState);
        //int GetIndex<S>();

        //IState GetState(int pIndex);
        //string GetNameState(int pIndex);

        // ***********************( Control de direccion )*********************** //
        void EnterFrom<S>(Action pFun) where S : IState;
        void EnterFrom(Type pType, Action pFun);

        internal void OnEnteringPossibleStates_internal<O>(MachineState<O> pMachine) where O : class;

        void FirstEnter();
        void Enter();
        void Exit();

        // ***********************( Metodos de Control )*********************** //
        void ManageEnter_internal<O>(MachineState<O> pMachine) where O : class;
        void ManageAfterEnter_internal<O>(MachineState<O> pMachine) where O : class;
        void ManagerExit_internal<O>(MachineState<O> pMachine) where O : class;
        void ManagerAfterExit_internal<O>(MachineState<O> pMachine) where O : class;


        // ***********************( Metodos Funcionales )*********************** //
        bool ChangeEnter_internal<S>(S pState) where S : IState;
        bool ChangeExit_internal<S>(S pState) where S : IState;
        int SaveIndex<O>(MachineState<O> pMachine) where O : class;
    }

    public static class StateExtensions
    {



        // ***********************( Metodos de Transiciones )*********************** //
        public static void EndTransition(this IState s, int pNext)
        {
            s.ChangeState(pNext);
        }
        public static void EndTrasition(this IState s, IState pNext)
        {
            s.ChangeState(pNext);
        }




        // ***********************( Metodos de Maquina )*********************** //
        public static (IState newS, IState oldS) ChangeState(this IState s, int pIndex)
            => s.Machine.ChangeState(pIndex);
        public static (IState newS, IState oldS) ChangeState(this IState s, IState pState)
            => s.Machine.ChangeState(s.Machine.GetIndex(pState));
        public static (IState newS, IState oldS) ChangeState<T>(this IState s)
            => s.Machine.ChangeState<T>();

        // TODO: ForceChanged.

        public static int GetIndex(this IState s, string pName) => s.Machine.GetIndex(pName);
        public static int GetIndex(this IState s, IState pState) => s.Machine.GetIndex(pState);
        public static int GetIndex<S>(this IState s) => s.Machine.GetIndex<S>();

        public static IState GetState(this IState s, int pIndex) => s.Machine.GetState(pIndex);
        public static string GetNameState(this IState s, int pIndex) => s.Machine.GetNameState(pIndex);
        public static int GetMyIndex(this IState s) => s.Index;

        public static O GetOwnerForMachine<O>(this IState s) where O : class
            => s.Machine.GetOwner<O>();

        public static int SaveIndex<O>(this IState s, MachineState<O> pMachine) where O : class
            => s.Index = pMachine.GetIndex(s);
    }



    public interface IStateLittle
    {
        void Update();
        void FixedUpdate();
    }

    public interface ITransitionState
    {
        Task Transition(CancellationToken pToken);
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
                    Debug.LogError($"(StateLittle->StateBase): 'Owner' is null, Please use it from Init.");
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












    public static class StateHelper
    {
        public static void RegisterDirectionality_internal(IState pState,
            Dictionary<Type, Action> pEnter, Dictionary<Type, Action> pExit)
        {
            pEnter.Clear();
            pExit.Clear();

            var methods = pState.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var m in methods)
            {
                foreach (var atr in m.GetCustomAttributes(true))
                {
                    if (atr is OnEnterFromAttribute enter)
                        pEnter[enter.Type] = (Action)Delegate.CreateDelegate(typeof(Action), pState, m);
                    else if (atr is OnExitToAttribute exit)
                        pExit[exit.Type] = (Action)Delegate.CreateDelegate(typeof(Action), pState, m);
                }
            }
        }

        public static bool InvokeFor(Dictionary<Type, Action> pMap, IState pOther)
        {
            if (pOther == null || pMap.Count == 0) return false;
            if (!pMap.TryGetValue(pOther.GetType(), out var action)) return false;
            action?.Invoke();
            return true;
        }

        public static void CallFirstEnter_internal(IState pState, ref bool pFlag)
        {
            if (ConsumeFirst_internal(ref pFlag))
                pState.FirstEnter();
        }
        public static bool ConsumeFirst_internal(ref bool pFlag)
        {
            bool was = pFlag;
            pFlag = false;
            return was;
        }

        // --- Corrutina de transición
        //public static void StartTransition_internal<O>(IState pState, MachineState<O> pMachine, ref Coroutine pCo)
        //    where O : class
        //{
        //    if (pState is ITransitionState t)
        //        pCo = pMachine.StartCoroutine(t.Transition());
        //}

        //public static void StopTransition_internal<O>(MachineState<O> pMachine, ref Coroutine pCo)
        //    where O : class
        //{
        //    if (pCo != null)
        //        pMachine.StopCoroutine(ref pCo);
        //}

        public static void StartTransition_internal(IState pState, ref CancellationTokenSource pCts)
        {
            if (pState is not ITransitionState t) return;

            StopTransition_internal(ref pCts);
            pCts = new CancellationTokenSource();
            _ = runTransition(t, pCts.Token);
        }

        public static void StopTransition_internal(ref CancellationTokenSource pCts)
        {
            if (pCts == null) return;
            pCts.Cancel();
            pCts.Dispose();
            pCts = null;
        }

        private static async Task runTransition(ITransitionState pT, CancellationToken pToken)
        {
            try
            {
                await pT.Transition(pToken);
            }
            catch (OperationCanceledException) { /* salida normal */ }
            catch (Exception e)
            {
                //MachineLog.Error?.Invoke($"(StateHelper): Transition failed: {e}");
            }
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