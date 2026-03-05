using System;
using System.Collections.Generic;

namespace FitVR.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service)
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                throw new Exception($"Service of type {type.Name} is already registered.");
            }

            _services[type] = service;
        }

        public static T Get<T>()
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            throw new Exception($"Service of type {type.Name} is not registered.");
        }

        public static void Unregister<T>()
        {
            var type = typeof(T);

            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        public static void Clear()
        {
            _services.Clear();
        }
    }
}



/* this is a simple version 

    Prevents double registration

    Throws clear errors if something missing

    Allows unregistering later (for scoped mini-games)

    Has Clear() for debugging
*/

/* Boring Definition of serviceLocator : 
    A service locator is a design pattern used in software development to manage and provide access to various services or dependencies within an application. 
    It acts as a central registry where services can be registered and retrieved by other parts of the application. 
    The main purpose of a service locator is to decouple the creation and management of services from the components that use them, allowing for more flexible and maintainable code.

    In a typical implementation, the service locator maintains a collection of services, often using a dictionary or similar data structure, 
    where the key is the type of the service and the value is the instance of that service. Components can request a service by its type, 
    and the service locator will return the corresponding instance.

    The service locator pattern can be useful in scenarios where you want to manage dependencies without using dependency injection frameworks, 
    but it can also lead to issues such as hidden dependencies and difficulties in testing if not used carefully.
*/

/* Short IRL scenario 
    a service like GameFlowManager will register itself on Awake() like this : 
        ServiceLocator.Register<GameFlowManager>(this);

    This GameFlowManger will have an Interface IGameFlowManager that it implements, 
    
    then other scripts can get it like this : 
        var gameFlowManager = ServiceLocator.Get<IGameFlowManager>();

        now they can call methods on the gameFlowManager without needing to know how it was created or where it is in the scene.
        gameFlowManager.LoadMiniGame();
*/

/* Alway keep dis bitch as a registery it only stores refs , it does not create or store services , savyyy ? */