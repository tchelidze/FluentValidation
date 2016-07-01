#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at https://github.com/jeremyskinner/FluentValidation
#endregion

namespace FluentValidation {
	using System;
	using System.ComponentModel;
	//using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using Internal;

	public static class ValidatorOptions {
		public static CascadeMode CascadeMode = CascadeMode.Continue;
		public static Type ResourceProviderType;

		private static ValidatorSelectorOptions validatorSelectorOptions = new ValidatorSelectorOptions();
		public static ValidatorSelectorOptions ValidatorSelectors { get { return validatorSelectorOptions; } }

		private static Func<Type, MemberInfo, LambdaExpression, string> propertyNameResolver = DefaultPropertyNameResolver;
		private static Func<Type, MemberInfo, LambdaExpression, string> displayNameResolver = DefaultDisplayNameResolver;

		public static Func<Type, MemberInfo, LambdaExpression, string> PropertyNameResolver {
			get { return propertyNameResolver; }
			set { propertyNameResolver = value ?? DefaultPropertyNameResolver; }
		}

		public static Func<Type, MemberInfo, LambdaExpression, string> DisplayNameResolver {
			get { return displayNameResolver; }
			set { displayNameResolver = value ?? DefaultDisplayNameResolver; }
		}

		static string DefaultPropertyNameResolver(Type type, MemberInfo memberInfo, LambdaExpression expression) {
			if (expression != null) {
				var chain = PropertyChain.FromExpression(expression);
				if (chain.Count > 0) return chain.ToString();
			}

			if (memberInfo != null) {
				return memberInfo.Name;
			}

			return null;
		}	
		
		static string DefaultDisplayNameResolver(Type type, MemberInfo memberInfo, LambdaExpression expression) {
			if (memberInfo == null) return null;
		    return GetDisplayName(memberInfo);
		}

		// Nasty hack to work around not referencing DataAnnotations directly. 
		// At some point investigate the DataAnnotations reference issue in more detail and go back to using the code above. 
		static string GetDisplayName(MemberInfo member) {
			var attributes = (from attr in member.GetCustomAttributes(true)
			                  select new {attr, type = attr.GetType()}).ToList();

			string name = null;

			name = (from attr in attributes
			        where attr.type.Name == "DisplayAttribute"
			        let method = attr.type.GetRuntimeMethod("GetName", new Type[0]) 
			        where method != null
			        select method.Invoke(attr.attr, null) as string).FirstOrDefault();

			if (string.IsNullOrEmpty(name)) {
				name = (from attr in attributes
				        where attr.type.Name == "DisplayNameAttribute"
				        let property = attr.type.GetRuntimeProperty("DisplayName")
				        where property != null
				        select property.GetValue(attr.attr, null) as string).FirstOrDefault();
			}

			return name;
		}
	}

	public class ValidatorSelectorOptions {
		private Func<IValidatorSelector>  defaultValidatorSelector = () => new DefaultValidatorSelector();
		private Func<string[], IValidatorSelector> memberNameValidatorSelector = properties => new MemberNameValidatorSelector(properties);
		private Func<string[], IValidatorSelector> rulesetValidatorSelector = ruleSets => new RulesetValidatorSelector(ruleSets);

		public Func<IValidatorSelector> DefaultValidatorSelectorFactory {
			get { return defaultValidatorSelector; }
			set { defaultValidatorSelector = value ?? (() => new DefaultValidatorSelector()); }
		}

		public Func<string[], IValidatorSelector> MemberNameValidatorSelectorFactory {
			get { return memberNameValidatorSelector; }
			set { memberNameValidatorSelector = value ?? (properties => new MemberNameValidatorSelector(properties)); }
		}

		public Func<string[], IValidatorSelector> RulesetValidatorSelectorFactory {
			get { return rulesetValidatorSelector; }
			set { rulesetValidatorSelector = value ?? (ruleSets => new RulesetValidatorSelector(ruleSets)); }
		}
	}
}