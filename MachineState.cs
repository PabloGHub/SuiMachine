using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Sui.Machine
{
    interface IMachineState
    {
        public StateBase State { get; set; }
        public int Count { get; }
        //private readonly int _identificador_i;

        void ActualizarTransiciones();
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

    /*
        // Problema: llama al Add de List en vez de ListState.
        var _estadoMoviendose = machineState.CreateState<EstadoMoviendose>();
        machineState.PosibleStates.Add(_estadoMoviendose);
     */



    /// <summary>
    /// --------------------------------------------------------------
    /// <br />
    /// Maquina de Estados  
    /// <br />
    /// --------------------------------------------------------------
    /// </summary>
    public class MachineState<O> : IMachineState where O : MonoBehaviour
    {
        // ***********************( ListState )*********************** //
        private class ListState : List<StateBase>
        {
            private readonly MachineState<O> _machineState;

            // Constructores.
            public ListState(MachineState<O> machineState)
            {
                _machineState = machineState;
            }

            public ListState(MachineState<O> machineState, IEnumerable<StateBase> collection) : base(collection)
            {
                _machineState = machineState;
                foreach (var item in collection)
                {
                    _machineState.GestionarEstado(item);
                }
            }

            // Getters y Setters.
            public IEnumerable<StateBase> Asignacion
            {
                get
                {
                    return new List<StateBase>(this);
                }
                set
                {
                    if (value == null)
                    {
                        Debug.LogError($"({_machineState._go.name}->MachineState): the collection is null.");
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
            public new void Add(StateBase item)
            {
                base.Add(item);
                _machineState?.GestionarEstado(item);
                item.AlEntrarEstadosPosibles(_machineState);
            }

            public new void AddRange(IEnumerable<StateBase> collection)
            {
                foreach (var item in collection)
                {
                    this.Add(item);
                }
            }
        }

        // ***********************( Variables/Declaraciones )*********************** //
        private StateBase _estadoActual { get; set; } = null; // Representa el estado actual de la maquina.

        private ListState _estadosPosibles { get; set; }
        [Obsolete("GetIndex ya recorre los estados posibles")]
        private Dictionary<string, int> _nombresEstados = new();
        private ListState _estadosPersistentes;

        private Dictionary<Func<bool>, StateBase> _transiciones = new();
        private GameObject _go;
        private O _source_O; // Script donde fue instanciada la maquina de estados.
        private GestionadorMachineState _gestionador_obj;

        // --- Control.
        private bool _activo_b = true;

        // --- Gestion.
        private List<int> _todosEstados = new();
        private int _crescendoId_i = 0;

        // ***********************( Getters y Setters )*********************** //
        public StateBase State
        {
            get { return _estadoActual; }
            set
            {
                if (_estadoActual == value)
                    return;

                if (_estadoActual != null)
                {
                    _estadoActual.GestionSalir();
                    if (!_estadoActual.f_CambioExit_b(value)) { _estadoActual.Exit(); }
                    _estadoActual.GestionTrasSalir();

                    _estadoActual.enabled = false;
                }

                var _estadoAnterior = value;
                _estadoActual = value;

                _estadoActual.enabled = true;
                OnStateChanged?.Invoke(_estadoActual);

                _estadoActual.GestionEntrar(this);
                if (!_estadoActual.f_CambioEnter_b(_estadoAnterior)) { _estadoActual.Enter(); }
                _estadoActual.GestionTrasEntrar();

                ActualizarTransiciones();
            }
        }


        /// <summary>
        /// get: Devuelve el estado en la posicion del indice.<br />
        /// set: Guarda el estado en la posicion del indice.<br />
        /// </summary>
        /// <param name="_indice_i"></param>
        /// <returns></returns>
        public StateBase this[int _indice_i]
        {
            get
            {
                if (_indice_i < 0 || _indice_i >= _estadosPosibles.Count)
                {
                    Debug.LogError($"({_go.name}->MachineState): El índice de estado es inválido.");
                    return null;
                }
                return _estadosPosibles[_indice_i];
            }
            set
            {
                if (_indice_i < 0)
                {
                    Debug.LogError($"({_go.name}->MachineState): El índice de estado es inválido.");
                    return;
                }
                _estadosPosibles[_indice_i] = value;
            }
        }

        /// <summary>
        /// get: Devuelve el indice del estado.<br />
        /// set: Guarda el estado en la posicion del indice.<br />
        /// </summary>
        /// <param name="_estado"></param>
        /// <returns></returns>
        public int this[StateBase _estado]
        {
            get
            {
                if (_estado == null)
                {
                    Debug.LogError($"({_go.name}->MachineState): El estado proporcionado es null.");
                    return -1;
                }
                return GetIndex(_estado);
            }
            set
            {
                if (_estado == null)
                {
                    Debug.LogError($"({_go.name}->MachineState): El estado proporcionado es null.");
                    return;
                }
                _estadosPosibles[value] = _estado;
            }
        }

        /// <summary>
        /// get: Devuelve la lista de estados posibles.<br />
        /// set: Sustituye la lista de estados posibles.<br />
        /// </summary>
        public List<StateBase> PosibleStates
        {
            get
            {
                return _estadosPosibles;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError($"({_go.name}->MachineState): La lista de estados posibles es null.");
                    return;
                }

                if (value.Count == 0)
                {
                    Debug.LogError($"({_go.name}->MachineState): La lista de estados posibles no puede estar vacía.");
                    return;
                }

                //annadirEstadosTodos(value);

                _estadosPosibles.Asignacion = (ListState)value;
            }
        }

        // TODO: Comprobar si el get fufa.
        /// <summary>
        /// get: Devuelve la lista de estados posibles.<br />
        /// set: Sustituye la lista de estados posibles.<br />
        /// </summary>
        public List<StateBase> this[List<StateBase> _estadosPosibles_obj]
        {
            get
            {
                return _estadosPosibles;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError($"({_go.name}->MachineState): The passed list of states is null.");
                    return;
                }

                if (value.Count == 0)
                {
                    Debug.LogError($"({_go.name}->MachineState): The passed list of states cannot be empty.");
                    return;
                }

                //annadirEstadosTodos(value);

                _estadosPosibles.Asignacion = (ListState)value;
            }
        }

        public string[] NamesStates
        {
            get
            {
                string[] nombres = new string[_estadosPosibles.Count];
                for (int i = 0; i < _estadosPosibles.Count; i++)
                {
                    if (_estadosPosibles[i] != null)
                    {
                        nombres[i] = _estadosPosibles[i].GetType().Name;
                    }
                }
                return nombres;
            }
        }

        public string NameStates2
        {
            get
            {
                return string.Join(", ", PosibleStates.Select(e => e.GetType().Name));
            }
        }

        /// <summary>
        /// get: Devuelve si la máquina de estados está activa.<br />
        /// set: Activa o desactiva la máquina de estados.<br />
        /// NOTA: No pasa por Enter ni Exit.
        /// </summary>
        public bool Active
        {
            get { return _activo_b; }
            set
            {
                if (_estadoActual != null)
                {
                    if (value == true)
                        _estadoActual.enabled = true;
                    else
                        _estadoActual.enabled = false;

                    _activo_b = value;
                }
                else
                    Debug.Log("(MachineState): State is NULL.");
            }
        }

        public int IndexState
        {
            get { return GetIndex(State); }
        }


        public int Count
        {
            get { return _estadosPosibles.Count; }
        }


        // ***********************( Eventos )*********************** //
        public event Action<StateBase> OnStateChanged;



        // ***********************( Metodos de Estados )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual de la máquina de estados.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state of the state machine.<br />
        /// </summary>
        /// <param name="_nuevoEstado_i">Es: Posicion en int del 'estadosPosibles' <br /> En: Position in int of 'PossibleStates'</param>
        public StateBase ChangeState(int _nuevoEstado_i)
        {
            if (_estadosPosibles == null)
                return null;

            if (!cambiarEstado(_nuevoEstado_i, out var _novoState_obj))
                return null;

            return _novoState_obj;
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual al respectivo nombre.<br />
        /// Añadalo primero a la lista o utilice 'CreateStateAutoAdd'.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state to the respective name.<br />
        /// Add it first to the list or use 'CreateStateAutoAdd'.<br />
        /// </summary>
        /// <param name="_novoEstado_s">Es: nombre del estado <br />En: name of state</param>
        /// <returns>Es: Retorna el nuevo estado cambiado <br />En: Returns the new changed state</returns>
        public StateBase ChangeState(string _novoEstado_s)
        {
            if (_estadosPosibles == null)
                return null;

            if (string.IsNullOrEmpty(_novoEstado_s))
            {
                Debug.LogError("(MachineState -> ChangeState): The name of the new state is null or empty.");
                return null;
            }

            if (!cambiarEstado(GetIndex(_novoEstado_s), out var _novoState_obj))
                return null;

            return _novoState_obj;
        }
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Cambia el estado actual al respectivo.<br />
        /// ___________________( English )___________________<br />
        /// Changes the current state to the respective.<br />
        /// </summary>
        /// <returns>Es: Retorna el nuevo estado cambiado <br />En: Returns the new changed state</returns>
        /// <typeparam name="T">Es: Tipo del estado a cambiar <br />En: Type of the state to change</typeparam>
        public StateBase ChangeState<T>()
        {
            return ChangeState(GetIndex(typeof(T).Name));
        }


        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        /// <param name="_nuevoEstado_i">'int' del estado a cambiar</param>
        /// <param name="_salida">nuevo estado al cambiado</param>
        /// <returns>false si no llego a cambiar</returns>
        private bool cambiarEstado(int _nuevoEstado_i, out StateBase _salida)
        {
            _salida = null;

            if (_nuevoEstado_i < 0 || _nuevoEstado_i >= _estadosPosibles.Count)
            {
                Debug.LogError($"({_go.name}->MachineState -> (internal)ChangeState): The index of the new state is out of range.");
                return false;
            }

            StateBase _posibleNovoEstado = _estadosPosibles[_nuevoEstado_i];
            if (_posibleNovoEstado == null)
            {
                Debug.LogError($"({_go.name}->MachineState -> (internal)ChangeState): Attempt to change state null");
                return false;
            }

            if (State == _posibleNovoEstado)
            {
                return false;
            }

            State = _posibleNovoEstado;
            _salida = State;
            return true;
        }

        [Obsolete]
        public List<string> GenerateNamesStates()
        {
            _nombresEstados.Clear();
            foreach (var _estadoIndividual in _estadosPosibles)
            {
                string _nombreEstado_s = _estadoIndividual.GetType().Name;
                if (!_nombresEstados.ContainsKey(_nombreEstado_s))
                    _nombresEstados.Add(_nombreEstado_s, GetIndex(_estadoIndividual));
            }
            return new List<string>(NamesStates);
        }
        [Obsolete]
        private List<string> generarNombresEstado(List<StateBase> value)
        {
            _nombresEstados.Clear();
            foreach (var _estadoIndividual in value)
            {
                string _nombreEstado_s = _estadoIndividual.GetType().Name;
                if (!_nombresEstados.ContainsKey(_nombreEstado_s))
                    _nombresEstados.Add(_nombreEstado_s, GetIndex(_estadoIndividual));
            }
            return new List<string>(NamesStates);
        }

        [Obsolete("Para eso esta ListState")]
        private void annadirEstadosTodos(List<StateBase> value)
        {
            foreach (var _estadoIndividual in value)
            {
                if (!_todosEstados.Contains(_estadoIndividual.Identificador))
                    _todosEstados.Add(_estadoIndividual.Identificador);
            }
        }

        [Obsolete]
        private List<string> f_generarYannadirEstadosTodos_List_s(List<StateBase> value)
        {
            _nombresEstados.Clear();
            foreach (var _estadoIndividual in value)
            {
                string _nombreEstado_s = _estadoIndividual.GetType().Name;
                if (!_nombresEstados.ContainsKey(_nombreEstado_s))
                    _nombresEstados.Add(_nombreEstado_s, GetIndex(_estadoIndividual));

                if (!_todosEstados.Contains(_estadoIndividual.Identificador))
                    _todosEstados.Add(_estadoIndividual.Identificador);
            }
            return new List<string>(NamesStates);
        }



        internal void GestionarEstado(StateBase e_estado)
        {
            if (e_estado != null)
            {
                if (e_estado.Identificador <= 0)
                    e_estado.Identificador = f_solicitarIde_i();
                if (!this._todosEstados.Contains(e_estado.Identificador))
                    this._todosEstados.Add(e_estado.Identificador);
            }
        }


        // NOTA: no se si dejarlo pues hace lo mismo que 'PosibleStates'.
        public List<StateBase> ChangeListSates(List<StateBase> _novoLista)
        {
            if (_novoLista == null)
            {
                Debug.LogError($"({_go.name}->MachineState): La nueva lista de estados posibles es nula.");
                return null;
            }
            if (_novoLista.Count == 0)
            {
                Debug.LogError($"({_go.name}->MachineState): La nueva lista de estados posibles esta vacía.");
                return null;
            }

            State = null;
            PosibleStates = _novoLista;

            return PosibleStates;
        }

        // ---( Asincronos )--- //
        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public async Task CambiarEstadoAsync(StateBase nuevoEstado)
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

        // ---( Persistentes )--- //
        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void AgregarEstadoPersistente(StateBase estado)
        {
            if (!_estadosPersistentes.Contains(estado))
            {
                _estadosPersistentes.Add(estado);
                //estado.MachineState = this;
                estado.enabled = true;
                estado.Enter();
            }
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void RemoverEstadoPersistente(StateBase estado)
        {
            if (_estadosPersistentes.Contains(estado))
            {
                estado.Exit();
                estado.enabled = false;
                _estadosPersistentes.Remove(estado);
            }
        }


        // ***********************( Metodos Forzar )*********************** //
        public void ForceExit()
        {
            State = null;
        }


        // ***********************( Metodos Gestion Ides )*********************** //
        private int f_solicitarIde_i()
        {
            return ++_crescendoId_i;
        }


        // ***********************( Metodos Limpieza Estados )*********************** //
        public void ClearImmediate(List<StateBase> _excluidosEspecificos = null)
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

            foreach (var _estadoIndividual in _estadosPosibles)
            {
                if (_estadoIndividual != null)
                    _idesExluidos.Add(_estadoIndividual.Identificador);
            }

            StateBase[] _todosEstados = _go.GetComponents<StateBase>();
            foreach (var _estadoIndividual in _todosEstados)
            {
                if (
                    !_idesExluidos.Contains(_estadoIndividual.Identificador) &&
                    _estadoIndividual != null// &&
                    //_estadoIndividual.MachineState == this
                    )
                {
                    _estadoIndividual.enabled = false;
                    _estadoIndividual.destroyThis();
                }
            }
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        /// <param name="_excluidosEspecificos"></param>
        public void Clear(List<StateBase> _excluidosEspecificos = null)
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

            foreach (var _estadoIndividual in _estadosPosibles)
            {
                if (_estadoIndividual != null)
                    _idesExluidos.Add(_estadoIndividual.Identificador);
            }

            // TODO: Borrar todos los estados UNO por cada ciclo de Unity.
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void Rm(List<StateBase> _listaBorrar)
        {
            // TODO: Borrar una lista de estados.
        }

        /// <summary>
        /// En Proceso de Fabricacion.
        /// </summary>
        public void Rm(StateBase _estadoBorrar)
        {
            // TODO: Borrar un estado.
        }


        // ***********************( Metodos Transiciones )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Agrega una transición a la máquina de estados. <br /> 
        /// Una transicion es una condición que, al cumplirse, cambia el estado actual de la máquina.<br />
        /// </summary>
        /// <param name="condicion">Es la condicion en lamda para cambiar '() => _parar_b == true'</param>
        /// <param name="estadoDestino">Estado al que cambiara pasando el int de la posicion de 'estadosPosibles'</param>
        public void AgregarTransicion(Func<bool> condicion, int estadoDestino)
        {
            if (_transiciones == null)
                _transiciones = new Dictionary<Func<bool>, StateBase>();

            if (estadoDestino < 0 || estadoDestino >= _estadosPosibles.Count)
            {
                Debug.LogError($"({_go.name}->MachineState): El índice de estado destino es inválido en AgregarTransicion.");
                return;
            }

            _transiciones[condicion] = _estadosPosibles[estadoDestino];
            //Debug.Log($"Transición agregada: {estadosPosibles[estadoDestino].GetType().Name}");
        }


        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Actualiza las transiciones de la máquina de estados.
        /// </summary>
        public void UpdateTransitions()
        {
            // Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAA");
            ActualizarTransiciones();
        }
        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        public void ActualizarTransiciones()
        {
            if (_transiciones == null)
                return;

            foreach (var transicion in _transiciones)
            {
                if (transicion.Key.Invoke())
                {
                    ChangeState(GetIndex(transicion.Value));
                    break;
                }
            }
        }

        // ***********************( Indices )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Obtiene el índice del estado en la lista de estados posibles.<br />
        /// ___________________( English )___________________<br />
        /// Gets the index of the state in the list of possible states.<br />
        /// </summary>
        /// <param name="estado">Es: Estado del que se quiere sacar el indice <br /> En: State from which to get the index</param>
        /// <returns>Retona un int del indice</returns>
        public int GetIndex(StateBase estado)
        {
            if (estado == null)
            {
                Debug.LogError($"({_go.name}->MachineState): El estado proporcionado es null.");
                return -1;
            }

            int indice = _estadosPosibles.IndexOf(estado);
            if (indice == -1)
            {
                Debug.LogWarning($"({_go.name}->MachineState): El estado {estado.GetType().Name} no se encuentra en la lista de estados posibles.");
            }

            return indice;
        }

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Obtiene el índice del estado en la lista de estados posibles.<br />
        /// ___________________( English )___________________<br />
        /// Gets the index of the state in the list of possible states.<br />
        /// </summary>
        /// <param name="nombreParaIndex">Es: nombre del estado <br /> En: name of state</param>
        /// <returns>Es: Retona un int del indice<br />En: Returns an int of the index</returns>
        public int GetIndex(string nombreParaIndex)
        {
            if (string.IsNullOrEmpty(nombreParaIndex))
            {
                Debug.LogError($"({_go.name}->MachineState): The name IsNullOrEmpty is true.");
                return -1;
            }

            foreach (var _estadoIndividual in _estadosPosibles)
            {
                string _nombreEstadoIndividual_s = _estadoIndividual.GetType().Name;
                if (nombreParaIndex.Equals(_nombreEstadoIndividual_s))
                {
                    return _estadosPosibles.IndexOf(_estadoIndividual);
                }
            }

            Debug.LogWarning($"({_go.name}->MachineState): The state {nombreParaIndex} not found on the list of states.");
            return -1;
        }

        [Obsolete]
        public int GetIndexObsoleto(string nombreEstado)
        {
            if (string.IsNullOrEmpty(nombreEstado))
            {
                Debug.LogError($"({_go.name}->MachineState): El nombre del estado proporcionado es nulo o vacío.");
                return -1;
            }
            GenerateNamesStates();
            if (_nombresEstados.TryGetValue(nombreEstado, out int indice))
            {
                return indice;
            }
            else
            {
                Debug.LogWarning($"({_go.name}->MachineState): El estado {nombreEstado} no se encuentra en la lista de nombres de estados.");
                return -1;
            }
        }

        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        public StateBase GetState(int _indice_i)
        {
            return null;
        }

        /// <summary>
        /// En proceso de fabricacion.
        /// </summary>
        public string GetNameState(int _indice_i)
        {
            return null;
        }

        // ***********************( Serealizacion )*********************** //
        // TODO: Implementar un sistema de serialización para guardar la máquina de estados.



        // ***********************( Funciones Constructores )*********************** //
        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Crea un nuevo estado de tipo T y lo inicializa con la dependencia proporcionada.<br />
        /// LLama al metodo Init() del estado.<br />
        /// -----------------<br />
        /// Importante: No añade a los estados posibles, Tendra que añadilos usted manualmente.<br />
        /// si quiero añadirlo automaticamente utilice 'CreateStateAutoAdd'.<br />
        /// </summary>
        /// <typeparam name="T">Estado que se quiera craer</typeparam>
        /// <returns>Es: Retorna el nuevo estado desactivado.</returns>
        public T CreateState<T>() where T : StateBase
        {
            return f_crearEstado_T<T>();
        }

        public static T CreateState<T>(GameObject e_go, O e_source_O, MachineState<O> e_ms) where T : StateBase
        {
            T estado = e_go.AddComponent<T>();
            estado.enabled = false;
            //estado.Identificador = f_solicitarIde_i();
            estado.Source = e_source_O;
            estado.ConstructorGestion(e_ms);
            estado.Init(e_source_O);

            return estado;
        }

        /// <summary>
        /// ___________________( Español )___________________<br />
        /// Crea un nuevo estado de tipo T y lo inicializa con la dependencia proporcionada.<br />
        /// LLama al metodo Init() del estado.<br />
        /// -----------------<br />
        /// Importante: Añade automaticamente a los estados posibles.<br />
        /// </summary>
        /// <typeparam name="T">Estado que se quiera craer</typeparam>
        public void CreateStateAutoAdd<T>() where T : StateBase
        {
            T novoEstado = f_crearEstado_T<T>();

            novoEstado.Identificador = f_solicitarIde_i();

            if (!_estadosPosibles.Contains(novoEstado))
                _estadosPosibles.Add(novoEstado);
            else
                Debug.LogWarning($"({_go.name}->MachineState): El estado {novoEstado.GetType().Name} ya existe en la lista de estados posibles.");
        }

        // TODO: Descubrir porque en medio del porceso salta un warging proveniente de GetIndex.
        // Porque llama a GetIndex (en ConstructorGestion) antes de añadirlo a estados posibles.
        private T f_crearEstado_T<T>() where T : StateBase
        {
            T estado = _go.AddComponent<T>();
            estado.enabled = false;
            //estado.MachineState = this;
            //estado.Identificador = f_solicitarIde_i();
            estado.Source = _source_O;
            estado.ConstructorGestion(this);
            estado.Init(_source_O);

            return estado;
        }

        // ***********************( Constructores )*********************** //
        public MachineState(GameObject goHost, List<StateBase> estadosPosibles, O _source_O)
        {
            PosibleStates = estadosPosibles ?? new List<StateBase>();

            inicializar(goHost, _source_O);
        }
        public MachineState(GameObject goHost, O _source_O)
        {
            inicializar(goHost, _source_O);
        }


        /// <summary>
        /// If you are not the MachinState developer, NEVER use anything in Spanish.
        /// </summary>
        private void inicializar(GameObject goHost, O _source_O)
        {
            if (goHost == null)
            {
                Debug.LogError($"({_go.name}->MachineState): El GameObject host es null en Inicializar.");
                return;
            }

            if (_source_O == null)
            {
                Debug.LogError($"({_go.name}->MachineState): La fuente de datos es null en Inicializar.");
                return;
            }

            _transiciones ??= new();
            _estadosPosibles ??= new(this);

            _go = goHost;
            this._source_O = _source_O;

            if (_gestionador_obj == null)
            {
                if (!_go.TryGetComponent<GestionadorMachineState>(out var gestionador))
                {
                    gestionador = _go.AddComponent<GestionadorMachineState>();
                    gestionador.MaquinasDeEstados.Add(this);
                }
                else
                {
                    if (!gestionador.MaquinasDeEstados.Contains(this))
                        gestionador.MaquinasDeEstados.Add(this);
                }

                _gestionador_obj = gestionador;
            }
        }
    }

    /// <summary>
    /// If you are not the MachinState developer, NEVER use anything in Spanish.
    /// </summary>
    public class GestionadorMachineState : MonoBehaviour
    {
        // ***********************( Variables )*********************** //
        internal List<IMachineState> MaquinasDeEstados;

        // ***********************( Eventos )*********************** //
        public Action OnUpdate;

        // ***********************( Unity )*********************** //
        private void Awake()
        {
            if (MaquinasDeEstados == null)
                MaquinasDeEstados = new List<IMachineState>();
        }

        private void FixedUpdate()
        {
            this.MaquinasDeEstados.ForEach(ms =>
            {
                if (ms.Count >= 1)
                    ms.ActualizarTransiciones();
            });

            OnUpdate?.Invoke();
        }

        // ***********************( Metodos )*********************** //
    }
}