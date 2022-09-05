/**
 * I think rust wants us to use lib for procedural macros?
 */

use proc_macro::TokenStream;
use quote::quote;


#[proc_macro_attribute]
pub fn dumb_attributes(attr: TokenStream, item: TokenStream) -> TokenStream {
    let attr = proc_macro2::TokenStream::from(attr);
    let item = proc_macro2::TokenStream::from(item);
    quote! {
        #[derive(Deserialize, Clone, Debug)]
        #[serde(rename = #attr)]
        #[serde(rename_all = "PascalCase")]
        #item
    }
    .into()
}
