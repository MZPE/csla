﻿//-----------------------------------------------------------------------
// <copyright file="ObjectContextManager.cs" company="Marimer LLC">
//     Copyright (c) Marimer LLC. All rights reserved.
//     Website: http://www.lhotka.net/cslanet/
// </copyright>
// <summary>Provides an automated way to reuse </summary>
//-----------------------------------------------------------------------
#if !MONO
using System;
using Microsoft.Data.Entity;

namespace Csla.Data.EF7
{
  /// <summary>
  /// Provides an automated way to reuse 
  /// Entity Framework DbContext objects 
  /// within the context
  /// of a single data portal operation.
  /// </summary>
  /// <typeparam name="C">
  /// Type of database 
  /// DbContext object to use.
  /// </typeparam>
  /// <remarks>
  /// This type stores the object context object 
  /// in <see cref="Csla.ApplicationContext.LocalContext" />
  /// and uses reference counting through
  /// <see cref="IDisposable" /> to keep the data context object
  /// open for reuse by child objects, and to automatically
  /// dispose the object when the last consumer
  /// has called Dispose."
  /// </remarks>
  public class DbContextManager<C> : IDisposable where C : DbContext
  {
    private static object _lock = new object();
    private C _context;
    private string _label;
    private string ContextLabel { get; set; }

    /// <summary>
    /// Gets the ObjectContextManager object for the     /// specified database.
    /// </summary>

    public static DbContextManager<C> GetManager()
    {
      return GetManager(string.Empty);
    }

    /// <summary>
    /// Gets the ObjectContextManager object for the 
    /// specified database.
    /// </summary>
    /// <param name="database">Database name as shown in the config file.</param>
    public static DbContextManager<C> GetManager(string database)
    {
      return GetManager(database, "default");
    }
   
    /// <summary>
    /// Gets the ObjectContextManager object for the 
    /// specified database.
    /// </summary>
    /// <param name="database">
    /// The database name or connection string.
    /// </param>
    /// <param name="label">Label for this context.</param>    
    /// <returns>ContextManager object for the name.</returns>
    public static DbContextManager<C> GetManager(string database, string label)
    {
      lock (_lock)
      {
        var contextLabel = GetContextName(database, label);
        DbContextManager<C> mgr = null;
        if (ApplicationContext.LocalContext.Contains(contextLabel))
        {
          mgr = (DbContextManager<C>)(ApplicationContext.LocalContext[contextLabel]);
        }
        else
        {
          mgr = new DbContextManager<C>(database, label);
          mgr.ContextLabel = contextLabel;
          ApplicationContext.LocalContext[contextLabel] = mgr;
        }
        mgr.AddRef();
        return mgr;
      }
    }      

    private DbContextManager(string database, string label)
    {
      _label = label;                
      if (string.IsNullOrEmpty(database))
      {
        _context = (C) (Activator.CreateInstance(typeof (C)));
      }
      else
      {
        _context = (C)(Activator.CreateInstance(typeof(C), database));
      }
    }

    private static string GetContextName(string database, string label)
    {
      return "__dbctx:" + label + "-" + database;
    }


    /// <summary>
    /// Gets the DbContext object.
    /// </summary>
    public C DbContext
    {
      get
      {
        return _context;
      }
    }

    #region  Reference counting

    private int _refCount;

    /// <summary>
    /// Gets the current reference count for this
    /// object.
    /// </summary>
    public int RefCount
    {
      get { return _refCount; }
    }

    private void AddRef()
    {
      _refCount += 1;
    }

    private void DeRef()
    {

      lock (_lock)
      {
        _refCount -= 1;
        if (_refCount == 0)
        {
          _context.Dispose();
          ApplicationContext.LocalContext.Remove(ContextLabel);
        }
      }

    }

    #endregion

    #region  IDisposable

    /// <summary>
    /// Dispose object, dereferencing or
    /// disposing the context it is
    /// managing.
    /// </summary>
    public void Dispose()
    {
      DeRef();
    }

    #endregion

  }
}
#endif