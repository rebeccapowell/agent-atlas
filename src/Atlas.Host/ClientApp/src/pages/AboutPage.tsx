export function AboutPage() {
  return (
    <div className="space-y-10">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">About Agent Atlas</h1>
        <p className="text-muted-foreground mt-2 text-lg">
          An internal map of capabilities for your organisation's APIs — designed for the world
          where people <em>and</em> software agents need to reliably discover and use your systems
          without tribal knowledge.
        </p>
      </div>

      <ArchitectureDiagram />

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

function ArchitectureDiagram() {
  return (
    <div className="rounded-lg border bg-muted/30 p-4">
      <p className="text-xs text-muted-foreground mb-3 text-center uppercase tracking-widest font-medium">
        Architecture Overview
      </p>
      <svg
        viewBox="0 0 860 420"
        width="100%"
        xmlns="http://www.w3.org/2000/svg"
        aria-label="Agent Atlas architecture diagram"
        role="img"
        className="text-foreground"
      >
        {/* ── color palette (CSS vars so light/dark both work) ── */}
        <defs>
          <marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
            <path d="M0,0 L0,6 L8,3 z" fill="currentColor" opacity="0.5" />
          </marker>
          <marker id="arrowBlue" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
            <path d="M0,0 L0,6 L8,3 z" fill="#3b82f6" />
          </marker>
          <marker id="arrowGreen" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
            <path d="M0,0 L0,6 L8,3 z" fill="#10b981" />
          </marker>
          <marker id="arrowOrange" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto">
            <path d="M0,0 L0,6 L8,3 z" fill="#f59e0b" />
          </marker>
        </defs>

        {/* ════════════════════════════════════════════════════════
            ROW 1 — PUBLISHERS
        ════════════════════════════════════════════════════════ */}
        {/* Team A */}
        <rect x="30" y="20" width="130" height="60" rx="8" fill="#f0fdf4" stroke="#86efac" strokeWidth="1.5" />
        <text x="95" y="43" textAnchor="middle" fontSize="11" fontWeight="600" fill="#166534">Team A</text>
        <text x="95" y="58" textAnchor="middle" fontSize="10" fill="#15803d">OpenAPI spec</text>
        <text x="95" y="72" textAnchor="middle" fontSize="10" fill="#15803d">x-atlas-tool: true</text>

        {/* Team B */}
        <rect x="180" y="20" width="130" height="60" rx="8" fill="#f0fdf4" stroke="#86efac" strokeWidth="1.5" />
        <text x="245" y="43" textAnchor="middle" fontSize="11" fontWeight="600" fill="#166534">Team B</text>
        <text x="245" y="58" textAnchor="middle" fontSize="10" fill="#15803d">OpenAPI spec</text>
        <text x="245" y="72" textAnchor="middle" fontSize="10" fill="#15803d">x-atlas-tool: true</text>

        {/* Team N */}
        <rect x="330" y="20" width="130" height="60" rx="8" fill="#f0fdf4" stroke="#86efac" strokeWidth="1.5" />
        <text x="395" y="43" textAnchor="middle" fontSize="11" fontWeight="600" fill="#166534">Team N …</text>
        <text x="395" y="58" textAnchor="middle" fontSize="10" fill="#15803d">OpenAPI spec</text>
        <text x="395" y="72" textAnchor="middle" fontSize="10" fill="#15803d">x-atlas-tool: true</text>

        {/* arrows: teams → git repo */}
        <line x1="95" y1="80" x2="240" y2="130" stroke="#86efac" strokeWidth="1.5" markerEnd="url(#arrow)" />
        <line x1="245" y1="80" x2="245" y2="130" stroke="#86efac" strokeWidth="1.5" markerEnd="url(#arrow)" />
        <line x1="395" y1="80" x2="255" y2="130" stroke="#86efac" strokeWidth="1.5" markerEnd="url(#arrow)" />

        {/* GitOps label on arrows */}
        <text x="95" y="112" fontSize="9" fill="#16a34a" fontStyle="italic">PR review</text>
        <text x="385" y="112" fontSize="9" fill="#16a34a" fontStyle="italic">PR review</text>

        {/* ════════════════════════════════════════════════════════
            ROW 2 — DATA PLANE GIT REPO
        ════════════════════════════════════════════════════════ */}
        <rect x="155" y="130" width="180" height="52" rx="8" fill="#eff6ff" stroke="#93c5fd" strokeWidth="1.5" />
        <text x="245" y="152" textAnchor="middle" fontSize="11" fontWeight="600" fill="#1d4ed8">Atlas Data Plane</text>
        <text x="245" y="167" textAnchor="middle" fontSize="10" fill="#2563eb">Git Repository (GitOps)</text>
        <text x="245" y="178" textAnchor="middle" fontSize="9" fill="#3b82f6">Helm / CI deploy on merge</text>

        {/* arrow: git repo → atlas core */}
        <line x1="245" y1="182" x2="430" y2="222" stroke="#3b82f6" strokeWidth="1.5" markerEnd="url(#arrowBlue)" />

        {/* ════════════════════════════════════════════════════════
            ROW 2 — IDENTITY PROVIDER
        ════════════════════════════════════════════════════════ */}
        <rect x="580" y="130" width="150" height="52" rx="8" fill="#fdf4ff" stroke="#d8b4fe" strokeWidth="1.5" />
        <text x="655" y="152" textAnchor="middle" fontSize="11" fontWeight="600" fill="#6b21a8">Identity Provider</text>
        <text x="655" y="167" textAnchor="middle" fontSize="10" fill="#7e22ce">OIDC / Entra / Keycloak</text>
        <text x="655" y="178" textAnchor="middle" fontSize="9" fill="#9333ea">Issues JWT tokens</text>

        {/* ════════════════════════════════════════════════════════
            ROW 3 — AGENT ATLAS CORE (central box, two halves)
        ════════════════════════════════════════════════════════ */}
        <rect x="300" y="225" width="320" height="110" rx="10" fill="#fff7ed" stroke="#fb923c" strokeWidth="2" />
        <text x="460" y="245" textAnchor="middle" fontSize="13" fontWeight="700" fill="#c2410c">Agent Atlas</text>

        {/* Catalog half */}
        <rect x="310" y="253" width="140" height="70" rx="6" fill="#fef3c7" stroke="#fcd34d" strokeWidth="1.2" />
        <text x="380" y="273" textAnchor="middle" fontSize="11" fontWeight="600" fill="#92400e">Catalog</text>
        <text x="380" y="289" textAnchor="middle" fontSize="9.5" fill="#b45309">Searchable tool inventory</text>
        <text x="380" y="303" textAnchor="middle" fontSize="9.5" fill="#b45309">Owners · scopes · safety tier</text>
        <text x="380" y="317" textAnchor="middle" fontSize="9.5" fill="#b45309">/v1/tools  /v1/apis</text>

        {/* MCP Proxy half */}
        <rect x="460" y="253" width="150" height="70" rx="6" fill="#fce7f3" stroke="#f9a8d4" strokeWidth="1.2" />
        <text x="535" y="273" textAnchor="middle" fontSize="11" fontWeight="600" fill="#9d174d">MCP Proxy</text>
        <text x="535" y="289" textAnchor="middle" fontSize="9.5" fill="#be185d">Code Mode interface</text>
        <text x="535" y="303" textAnchor="middle" fontSize="9.5" fill="#be185d">search · describe · execute</text>
        <text x="535" y="317" textAnchor="middle" fontSize="9.5" fill="#be185d">/mcp  (requires OIDC)</text>

        {/* IdP → Atlas auth arrow */}
        <line x1="580" y1="178" x2="535" y2="253" stroke="#a855f7" strokeWidth="1.5" strokeDasharray="5,3" markerEnd="url(#arrow)" />
        <text x="572" y="218" fontSize="9" fill="#9333ea" fontStyle="italic">validates JWT</text>

        {/* ════════════════════════════════════════════════════════
            ROW 4 — CONSUMERS
        ════════════════════════════════════════════════════════ */}
        {/* People / UI consumer */}
        <rect x="140" y="370" width="160" height="42" rx="8" fill="#f0f9ff" stroke="#7dd3fc" strokeWidth="1.5" />
        <text x="220" y="388" textAnchor="middle" fontSize="11" fontWeight="600" fill="#075985">Developers / People</text>
        <text x="220" y="404" textAnchor="middle" fontSize="9.5" fill="#0369a1">Browse catalog (read-only UI)</text>

        {/* Agent / Copilot consumer */}
        <rect x="330" y="370" width="160" height="42" rx="8" fill="#fdf4ff" stroke="#d8b4fe" strokeWidth="1.5" />
        <text x="410" y="388" textAnchor="middle" fontSize="11" fontWeight="600" fill="#6b21a8">Agents / Copilot</text>
        <text x="410" y="404" textAnchor="middle" fontSize="9.5" fill="#7e22ce">MCP Code Mode (search+exec)</text>

        {/* Downstream APIs */}
        <rect x="630" y="340" width="180" height="52" rx="8" fill="#fff1f2" stroke="#fca5a5" strokeWidth="1.5" />
        <text x="720" y="360" textAnchor="middle" fontSize="11" fontWeight="600" fill="#991b1b">Downstream APIs</text>
        <text x="720" y="375" textAnchor="middle" fontSize="9.5" fill="#dc2626">Your real services</text>
        <text x="720" y="389" textAnchor="middle" fontSize="9.5" fill="#dc2626">Enforce business authz</text>

        {/* Catalog → People arrow */}
        <line x1="380" y1="335" x2="265" y2="370" stroke="#0ea5e9" strokeWidth="1.5" markerEnd="url(#arrowBlue)" />

        {/* MCP Proxy → Agent arrow */}
        <line x1="480" y1="335" x2="440" y2="370" stroke="#a855f7" strokeWidth="1.5" markerEnd="url(#arrow)" />

        {/* Atlas MCP Proxy → Downstream APIs (execute + pass-through JWT) */}
        <line x1="620" y1="290" x2="630" y2="355" stroke="#ef4444" strokeWidth="1.5" markerEnd="url(#arrowOrange)" />
        <text x="640" y="328" fontSize="9" fill="#ef4444" fontStyle="italic">forwards caller JWT</text>

        {/* ════════════════════════════════════════════════════════
            LEGEND
        ════════════════════════════════════════════════════════ */}
        <rect x="30" y="370" width="100" height="42" rx="6" fill="none" stroke="currentColor" strokeWidth="0.8" opacity="0.3" />
        <text x="80" y="385" textAnchor="middle" fontSize="9" fill="currentColor" opacity="0.6" fontWeight="600">Legend</text>
        <line x1="38" y1="396" x2="60" y2="396" stroke="#86efac" strokeWidth="1.5" />
        <text x="63" y="399" fontSize="8.5" fill="currentColor" opacity="0.7">GitOps publish</text>
        <line x1="38" y1="406" x2="60" y2="406" stroke="#ef4444" strokeWidth="1.5" />
        <text x="63" y="409" fontSize="8.5" fill="currentColor" opacity="0.7">JWT pass-through</text>
      </svg>
    </div>
  )
}
