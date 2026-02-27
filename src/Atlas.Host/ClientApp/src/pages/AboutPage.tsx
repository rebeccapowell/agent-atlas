export function AboutPage() {
  return (
    <div className="max-w-3xl space-y-10">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">About Agent Atlas</h1>
        <p className="text-muted-foreground mt-2 text-lg">
          An internal map of capabilities for your organisation's APIs — designed for the world
          where people <em>and</em> software agents need to reliably discover and use your systems
          without tribal knowledge.
        </p>
      </div>

      <Section title="What problem it solves">
        <p>
          In most enterprises, APIs exist everywhere but they're hard to use safely at scale. Teams
          publish endpoints, docs drift, permissions are unclear, and when an agent or developer
          wants to "do the thing" — create an order, look up meters, retrieve a customer record —
          they need to know which service owns it, what the inputs look like, what permissions are
          required, and how to chain calls together.
        </p>
        <p className="mt-3">
          Agent Atlas turns that sprawl into an operationally governed product: a single catalog
          that tells humans and agents "these are the tools our organisation offers" and a secure
          proxy that can execute those tools using the caller's existing identity.
        </p>

        <SubSection title="1) Capability discovery">
          Even in mature organisations, people struggle to answer basic questions: "Which API can do
          X?", "What's the endpoint for Y?", "Which service team owns it?", "Is it safe to call?",
          "What do I need access to?". Documentation lives in wikis, runbooks, or scattered OpenAPI
          files, and quickly becomes out of date.
        </SubSection>

        <SubSection title="2) Making APIs usable by agents">
          Agents don't work well with random documentation or large, messy API surfaces. They need
          a structured way to discover and call operations — what the operation is, what parameters
          are required, and what it returns. Agent Atlas presents your API capabilities explicitly
          as "tools" that an agent can search and invoke, while keeping the control of
          authorisation with the owning team.
        </SubSection>

        <SubSection title="3) Governance without bureaucracy">
          Enterprises need approvals, audit trails, and predictable change control, but they also
          need speed. Agent Atlas uses GitOps principles: changes to what's published as "agent
          tools" are made via pull requests, reviewed, and then deployed. This makes the tool
          catalog part of the same engineering discipline as code.
        </SubSection>
      </Section>

      <Section title="What it is, in plain terms">
        <p>Agent Atlas is two things packaged as one product:</p>

        <SubSection title="A catalog">
          A curated inventory of the APIs and endpoints that your organisation considers
          "agent-ready." Teams publish OpenAPI specs into a shared data-plane repository. Within
          those specs, they mark the endpoints they want exposed as tools and document what
          permissions are required. Agent Atlas reads that repository and materialises a searchable
          catalog of tools.
        </SubSection>

        <SubSection title='A proxy that speaks MCP "Code Mode"'>
          Agents like Copilot increasingly use a protocol called MCP (Model Context Protocol) to
          interact with tools. Instead of exposing hundreds or thousands of tools individually,
          Agent Atlas provides a small, stable interface: search for tools, describe a tool, and
          execute a plan that invokes tools. This keeps the agent interaction predictable and
          scalable even as your API estate grows.
        </SubSection>
      </Section>

      <Section title="How teams publish tools">
        <p>
          In the Agent Atlas model, teams don't register endpoints through a web form or admin UI.
          Instead, they contribute in the same way they contribute code:
        </p>
        <ul className="mt-3 space-y-2 list-disc list-inside text-muted-foreground">
          <li>
            Submit an OpenAPI spec (or update) to the organisation's "Atlas data plane" Git
            repository.
          </li>
          <li>
            In the spec, tag certain operations as tools and include metadata such as the stable
            tool name, safety tier (read / write / destructive), and required scopes, roles, and
            permissions.
          </li>
          <li>
            A pull request is reviewed and approved (e.g. by a platform team member or via
            CODEOWNERS).
          </li>
          <li>
            When merged, the Atlas deployment is updated so the catalog reflects the new state.
          </li>
        </ul>
        <p className="mt-3">
          This is a strong governance model: it's auditable, reviewable, and reversible. Rollbacks
          are just Git or Helm rollbacks.
        </p>
      </Section>

      <Section title="How users and agents consume it">
        <SubSection title="For people">
          A simple read-only UI answers: What APIs are registered? What tools are available? What
          endpoints do they map to? What permissions do they require? Who owns them? This is a
          "capability map" for the organisation, useful for onboarding, cross-team integration, and
          operational clarity.
        </SubSection>

        <SubSection title="For agents (Copilot, internal assistants, automation)">
          Agent Atlas exposes an MCP interface where an agent can search for tools by intent
          ("meters", "create offer", "customer lookup"), retrieve structured details about a tool
          (inputs, outputs, safety tier, required permissions), and execute multi-step calls using
          a plan. Instead of navigating thousands of APIs or brittle documentation, the agent has a
          consistent "tool surface" across the entire enterprise.
        </SubSection>
      </Section>

      <Section title="Security model: safe by design, without becoming a bottleneck">
        <p>
          Agent Atlas is deliberately not the place where business authorisation decisions happen —
          that stays inside the APIs owned by teams. However, Atlas must still be secure, so it
          does two things:
        </p>

        <SubSection title="1) It authenticates and authorises use of Atlas itself">
          To use the catalog or execute tools through Atlas, callers must present valid OIDC tokens
          issued by your identity provider. Atlas enforces platform-level permissions such as{" "}
          <code className="bg-muted px-1 rounded text-sm">platform-code-mode:search</code> and{" "}
          <code className="bg-muted px-1 rounded text-sm">platform-code-mode:execute</code>,
          preventing Atlas from being used as an open relay.
        </SubSection>

        <SubSection title="2) It passes the caller's token through to the downstream API">
          When an agent executes a tool, Atlas forwards the caller's JWT to the downstream API.
          The downstream API then enforces the real business permissions. Atlas does not "grant
          access" to data — it simply routes the authenticated caller's request. This keeps
          ownership aligned: the platform team owns discovery and safe proxying; product teams own
          authorisation and data protection.
        </SubSection>
      </Section>

      <Section title="Why published scopes and roles still matter">
        <p>
          Even though Atlas doesn't enforce downstream permissions, publishing the required scopes
          and roles is a high-value feature:
        </p>
        <ul className="mt-3 space-y-2 list-disc list-inside text-muted-foreground">
          <li>It makes permissions transparent: users can see what they need to request.</li>
          <li>
            It supports self-service: "you're missing X; request access package Y."
          </li>
          <li>
            It improves agent behaviour: an agent can choose tools the user likely has access to,
            or explain why an action failed.
          </li>
        </ul>
        <p className="mt-3">
          Atlas can optionally compute "likely accessible" tools by comparing the permissions
          declared in the tool metadata against claims in the user's token. This remains
          best-effort and informational, but it's a powerful UX improvement.
        </p>
      </Section>

      <Section title="Why it matters strategically">
        <p>
          From a product and executive perspective, Agent Atlas is an enabling platform for the
          next operating model of software delivery:
        </p>
        <ul className="mt-3 space-y-2 list-disc list-inside text-muted-foreground">
          <li>
            <strong className="text-foreground">Agents will be a major interface</strong> to
            enterprise systems, not just dashboards and internal UIs.
          </li>
          <li>
            <strong className="text-foreground">Discoverability and consistency</strong> become a
            foundational capability: without it, agent adoption will be chaotic, brittle, and
            unsafe.
          </li>
          <li>
            <strong className="text-foreground">GitOps governance</strong> ensures you can scale
            tool exposure without a centralised admin bottleneck.
          </li>
          <li>
            <strong className="text-foreground">Reduced integration friction</strong> means faster
            delivery: teams don't need to know everything; they can find it.
          </li>
        </ul>
        <p className="mt-3">
          Think of it as the "service catalog" concept (like Backstage) but optimised for agent
          tool use: it's not just "what services exist" — it's "what actions can be performed
          safely, and how."
        </p>
      </Section>

      <Section title="What success looks like">
        <p>
          In a mature deployment, Agent Atlas becomes the default answer to questions like "Can we
          do this through an API?", "Which endpoint is the approved one?", "What do I need to
          request access to?", and "How can Copilot or other agents safely operate our systems?"
        </p>
        <p className="mt-3">It enables:</p>
        <ul className="mt-2 space-y-2 list-disc list-inside text-muted-foreground">
          <li>Consistent agent experiences across domains</li>
          <li>Shared patterns for tool annotation and publication</li>
          <li>Measurable tool usage and reliability improvements over time</li>
        </ul>
        <p className="mt-3">
          In short: Agent Atlas turns a messy API estate into an organised, governed set of
          capabilities that both humans and AI agents can use safely and efficiently.
        </p>
      </Section>
    </div>
  )
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="space-y-3">
      <h2 className="text-xl font-semibold tracking-tight border-b pb-2">{title}</h2>
      <div className="text-muted-foreground leading-relaxed">{children}</div>
    </section>
  )
}

function SubSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mt-4">
      <h3 className="text-base font-medium text-foreground mb-1">{title}</h3>
      <p className="text-muted-foreground leading-relaxed">{children}</p>
    </div>
  )
}
