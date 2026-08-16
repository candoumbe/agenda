/** Generic representation of a browsable resource returned by the API. */
export interface Browsable<TResource> {
  resource: TResource;
  links: { href: string; method?: string; relations?: string[] }[];
}
