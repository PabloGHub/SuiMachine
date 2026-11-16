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



    public interface IMachineState
    {
        IState State { get; set; }
        int Count { get; }

        int Identifier { get; set; }

        O GetOwner<O>() where O : MonoBehaviour;

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
        void StopCoroutine(ref Coroutine pCoroutine);
        Coroutine StartCoroutine(IEnumerator pEnumerator);
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


    // TODO: Implementar de forma explicita muchas funciones internas.
    public interface IState
    {
        // ***********************( Getter, Setters e Indesxadores )*********************** //
        MonoBehaviour Owner { get; }

        internal MonoBehaviour Owner_internal { get; set; }

        O GetOwner<O>() where O : MonoBehaviour => Owner as O;

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
        event Action OnFirtsEnter;
        event Action<int> OnChangeId;

        event Action<int> ChangeInt;
        event Action<IState> ChangeIState;

        // ***********************( Contructores )*********************** //
        void ManagerBuilder_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;
        void Init<T>(T owner);
        void Init();

        // ***********************( Metodos de Transiciones )*********************** //
        void EndTransition(int pNext);
        void EndTrasition(IState pNext);

        // ***********************( Metodos de Maquina )*********************** //
        (IState newS, IState oldS) ChangeState(int pStateIndex);
        (IState newS, IState oldS) ChangeState(IState pState);
        (IState newS, IState oldS) ChangeState<T>();

        // TODO: ForceChanged.

        int GetMyIndex();
        int GetIndex(string eName);
        int GetIndex(IState pState);
        int GetIndex<S>();

        IState GetState(int pIndex);
        string GetNameState(int pIndex);

        // ***********************( Control de direccion )*********************** //
        void EnterFrom<S>(Action pFun) where S : IState;
        void EnterFrom(Type pType, Action pFun);

        internal void OnEnteringPossibleStates_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;

        void Enter();
        void Exit();

        // ***********************( Metodos de Control )*********************** //
        internal void ManageEnter_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;
        internal void ManageAfterEnter_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;
        internal void ManagerExit_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;
        internal void ManagerAfterExit_internal<O>(MachineState<O> pMachine) where O : MonoBehaviour;

        // void JumpEnter();
        // void JumpExit();

        // ***********************( Metodos Funcionales )*********************** //
        internal bool ChangeEnter_internal<S>(S pState) where S : IState;
        internal bool ChangeExit_internal<S>(S pState) where S : IState;
        int SaveIndex<O>(MachineState<O> pMachine) where O : MonoBehaviour;
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


}