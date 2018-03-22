/*
 *  Copyright (c) 2014-Present, Facebook, Inc.
 *  All rights reserved.
 *
 *  This source code is licensed under the BSD-style license found in the
 *  LICENSE file in the root directory of this source tree. An additional grant
 *  of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JavaScriptEngineSwitcher.Core;
using Newtonsoft.Json;
using React.Exceptions;

namespace React
{
	/// <summary>
	/// Represents a React JavaScript component.
	/// </summary>
	public class ReactComponent : IReactComponent
	{
		private static readonly ConcurrentDictionary<string, bool> _componentNameValidCache = new ConcurrentDictionary<string, bool>();

		[ThreadStatic]
		private static StringBuilder _sharedSb;

		/// <summary>
		/// Regular expression used to validate JavaScript identifiers. Used to ensure component
		/// names are valid.
		/// Based off https://gist.github.com/Daniel15/3074365
		/// </summary>
		private static readonly Regex _identifierRegex = new Regex(@"^[a-zA-Z_$][0-9a-zA-Z_$]*(?:\[(?:"".+""|\'.+\'|\d+)\])*?$", RegexOptions.Compiled);

		/// <summary>
		/// Environment this component has been created in
		/// </summary>
		protected readonly IReactEnvironment _environment;

		/// <summary>
		/// Global site configuration
		/// </summary>
		protected readonly IReactSiteConfiguration _configuration;

		/// <summary>
		/// Raw props for this component
		/// </summary>
		protected object _props;

		/// <summary>
		/// JSON serialized props for this component
		/// </summary>
		protected string _serializedProps;

		/// <summary>
		/// Gets or sets the name of the component
		/// </summary>
		public string ComponentName { get; set; }

		/// <summary>
		/// Gets or sets the unique ID for the DIV container of this component
		/// </summary>
		public string ContainerId { get; set; }

		/// <summary>
		/// Gets or sets the HTML tag the component is wrapped in
		/// </summary>
		public string ContainerTag { get; set; }

		/// <summary>
		/// Gets or sets the HTML class for the container of this component
		/// </summary>
		public string ContainerClass { get; set; }

		/// <summary>
		/// Get or sets if this components only should be rendered server side
		/// </summary>
		public bool ServerOnly { get; set; }

		/// <summary>
		/// Gets or sets the props for this component
		/// </summary>
		public object Props
		{
			get { return _props; }
			set
			{
				_props = value;
				_serializedProps = JsonConvert.SerializeObject(
					value,
					_configuration.JsonSerializerSettings
				);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReactComponent"/> class.
		/// </summary>
		/// <param name="environment">The environment.</param>
		/// <param name="configuration">Site-wide configuration.</param>
		/// <param name="componentName">Name of the component.</param>
		/// <param name="containerId">The ID of the container DIV for this component</param>
		public ReactComponent(IReactEnvironment environment, IReactSiteConfiguration configuration, string componentName, string containerId)
		{
			EnsureComponentNameValid(componentName);
			_environment = environment;
			_configuration = configuration;
			ComponentName = componentName;
			ContainerId = string.IsNullOrEmpty(containerId) ? ReactIdGenerator.Generate() : containerId;
			ContainerTag = "div";
		}

		/// <summary>
		/// Renders the HTML for this component. This will execute the component server-side and
		/// return the rendered HTML.
		/// </summary>
		/// <param name="renderContainerOnly">Only renders component container. Used for client-side only rendering.</param>
		/// <param name="renderServerOnly">Only renders the common HTML mark up and not any React specific data attributes. Used for server-side only rendering.</param>
		/// <param name="exceptionHandler">A custom exception handler that will be called if a component throws during a render. Args: (Exception ex, string componentName, string containerId)</param>
		/// <returns>HTML</returns>
		public string RenderHtml(bool renderContainerOnly = false, bool renderServerOnly = false, Action<Exception, string, string> exceptionHandler = null)
		{
			var writer = new StringWriter();
			RenderHtml(writer, renderContainerOnly, renderServerOnly, exceptionHandler);
			return writer.ToString();
		}

		/// <summary>
		/// Renders the HTML for this component. This will execute the component server-side and
		/// return the rendered HTML.
		/// </summary>
		/// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to which the content is written</param>
		/// <param name="renderContainerOnly">Only renders component container. Used for client-side only rendering.</param>
		/// <param name="renderServerOnly">Only renders the common HTML mark up and not any React specific data attributes. Used for server-side only rendering.</param>
		/// <param name="exceptionHandler">A custom exception handler that will be called if a component throws during a render. Args: (Exception ex, string componentName, string containerId)</param>
		/// <returns>HTML</returns>
		public virtual void RenderHtml(TextWriter writer, bool renderContainerOnly = false, bool renderServerOnly = false, Action<Exception, string, string> exceptionHandler = null)
		{
			if (!_configuration.UseServerSideRendering)
			{
				renderContainerOnly = true;
			}

			if (!renderContainerOnly)
			{
				EnsureComponentExists();
			}

			var html = string.Empty;
			if (!renderContainerOnly)
			{
				var stringBuilder = _sharedSb;
				_sharedSb = null;

				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(_serializedProps.Length + ComponentName.Length + 100);
				}

				try
				{
					var stringWriter = new StringWriter(stringBuilder);
					stringWriter.Write(renderServerOnly ? "ReactDOMServer.renderToStaticMarkup(" : "ReactDOMServer.renderToString(");
					WriteComponentInitialiser(stringWriter);
					stringWriter.Write(")");

					html = _environment.Execute<string>(stringWriter.ToString());

					if (renderServerOnly)
					{
						writer.Write(html);
						return;
					}
				}
				catch (JsRuntimeException ex)
				{
					if (exceptionHandler == null)
					{
						exceptionHandler = _configuration.ExceptionHandler;
					}

					exceptionHandler(ex, ComponentName, ContainerId);
				}
				finally
				{
					stringBuilder.Clear();
					_sharedSb = stringBuilder;
				}
			}
			
			writer.Write("<");
			writer.Write(ContainerTag);
			writer.Write(" ");
			
			writer.Write("id=\"");
			writer.Write(ContainerId);
			writer.Write("\"");
			if (!string.IsNullOrEmpty(ContainerClass))
			{
				writer.Write(" class=\"");
				writer.Write(ContainerClass);
				writer.Write("\"");
			}
			
			writer.Write(">");
			writer.Write(html);
			writer.Write("</");
			writer.Write(ContainerTag);
			writer.Write(">");
		}

		/// <summary>
		/// Renders the JavaScript required to initialise this component client-side. This will
		/// initialise the React component, which includes attach event handlers to the
		/// server-rendered HTML.
		/// </summary>
		/// <returns>JavaScript</returns>
		public string RenderJavaScript()
		{
			var writer = new StringWriter();
			RenderJavaScript(writer);
			return writer.ToString();
		}
		
		/// <summary>
		/// Renders the JavaScript required to initialise this component client-side. This will
		/// initialise the React component, which includes attach event handlers to the
		/// server-rendered HTML.
		/// </summary>
		/// <returns>JavaScript</returns>
		public virtual void RenderJavaScript(TextWriter writer)
		{
			writer.Write("ReactDOM.hydrate(");
			WriteComponentInitialiser(writer);
			writer.Write(", document.getElementById(\"");
			writer.Write(ContainerId);
			writer.Write("\"))");
		}

		/// <summary>
		/// Ensures that this component exists in global scope
		/// </summary>
		protected virtual void EnsureComponentExists()
		{
			// This is safe as componentName was validated via EnsureComponentNameValid()
			var componentExists = _environment.Execute<bool>(string.Format(
				"typeof {0} !== 'undefined'",
				ComponentName
			));
			if (!componentExists)
			{
				throw new ReactInvalidComponentException(string.Format(
					"Could not find a component named '{0}'. Did you forget to add it to " +
					"App_Start\\ReactConfig.cs?",
					ComponentName
				));
			}
		}

		/// <summary>
		/// Gets the JavaScript code to initialise the component
		/// </summary>
		/// <param name="writer">The <see cref="T:System.IO.TextWriter" /> to which the content is written</param>
		protected virtual void WriteComponentInitialiser(TextWriter writer)
		{
			writer.Write("React.createElement(");
			writer.Write(ComponentName);
			writer.Write(", ");
			writer.Write(_serializedProps);
			writer.Write(")");
		}

		/// <summary>
		/// Validates that the specified component name is valid
		/// </summary>
		/// <param name="componentName"></param>
		internal static void EnsureComponentNameValid(string componentName)
		{
			var isValid = _componentNameValidCache.GetOrAdd(componentName, compName => compName.Split('.').All(segment => _identifierRegex.IsMatch(segment)));
			if (!isValid)
			{
				throw new ReactInvalidComponentException($"Invalid component name '{componentName}'");
			}
		}
	}
}
