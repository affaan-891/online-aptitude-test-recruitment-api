# High-Difficulty Academic Viva Voce Defense Questions & Model Answers
## Webster Organisation - Online Aptitude Test & Recruitment System

---

### Question 1: ACID Transaction Isolation & Race Conditions in Assessment Scoring
**Examiner Question:**
> *"In your stored procedure `sp_SubmitSectionAnswers`, explain why you used `BEGIN TRANSACTION` with row-level update locks (`UPDLOCK, ROWLOCK`), and how this prevents race conditions if a candidate fires simultaneous asynchronous HTTP requests to submit answers."*

**Model Answer:**
In high-stakes online examinations, a candidate or an automated client tool could trigger duplicate concurrent HTTP `POST` requests to submit answers. Without transactional concurrency control, this creates a **race condition** (Lost Updates or Phantom Responses).

In `sp_SubmitSectionAnswers`:
1. `SET XACT_ABORT ON` ensures that if any runtime error or constraint violation occurs, the entire transaction is rolled back immediately, leaving no orphaned responses.
2. The initial query uses `WITH (UPDLOCK, ROWLOCK)` when reading from `dbo.TestAttempts`. An `UPDLOCK` (Update Lock) tells SQL Server that this transaction intends to modify the row. Unlike a shared lock (`NOLOCK` or standard `S` lock), an update lock is mutually exclusive with other update locks on the same resource.
3. If Request B arrives while Request A is executing, Request B is blocked until Request A commits or rolls back.
4. If Request A successfully advances the `CurrentSectionId` or marks the attempt completed, Request B will subsequently read the updated status (`CurrentSectionId <> @SubmittedSectionId`) and fail validation gracefully, preventing double-marking or inconsistent state transitions.

---

### Question 2: Two-Tier Defense of Linear Stage Gating (Trigger vs Action Filter)
**Examiner Question:**
> *"Why did you implement linear progression enforcement twice — once in the C# action filter `ValidateTestLinearityAttribute` and once in the database trigger `trg_EnforceLinearSectionFlow`? Isn't this redundant code?"*

**Model Answer:**
This exemplifies the **Defense-in-Depth (DiD)** architectural principle:
- **Presentation/API Tier (`ValidateTestLinearityAttribute`)**:
  Interprets incoming HTTP payloads before invoking controller or service business logic. Catching stage violations at the filter level fails fast (HTTP 400 Bad Request) without consuming database connection pool threads, opening transactions, or generating database lock overhead.
- **Data Persistence Tier (`trg_EnforceLinearSectionFlow`)**:
  Guarantees absolute relational invariant integrity at the single source of truth. If an administrator executes an ad-hoc SQL query, a rogue script runs, or an internal microservice bypasses the Web API, the database trigger executes `AFTER UPDATE` and immediately calls `ROLLBACK TRANSACTION` if an attempt tries to regress (`s_new.SequenceOrder < s_old.SequenceOrder`) or skip stages.
- Therefore, the filter provides **high-throughput performance protection**, while the trigger guarantees **immutable data integrity**.

---

### Question 3: Anti-Tampering Mechanism for Client Countdown Timers
**Examiner Question:**
> *"How do you prevent a candidate from modifying their browser's JavaScript variables or manipulating their computer system clock to gain extra time during the General Knowledge or Math rounds?"*

**Model Answer:**
The client-side timer is purely a **visual user experience indicator**; the backend operates on an **authoritative server timestamp**:
1. When a section begins, the server records the exact UTC timestamp in `TestAttempts.SectionStartedAt` using `SYSUTCDATETIME()`.
2. When the candidate submits answers via `POST /api/assessment/submit-section`, the server recalculates the elapsed time dynamically:
   $$\text{ElapsedSeconds} = \text{DATEDIFF}(\text{SECOND}, \text{SectionStartedAt}, \text{SYSUTCDATETIME}())$$
3. The server compares this elapsed time against $(\text{TimeLimitMinutes} \times 60) + \text{GracePeriod}$ (15 seconds network latency buffer).
4. Even if a candidate pauses browser execution in Chrome DevTools or manipulates their local clock by hours, the server calculates elapsed time against its own internal hardware clock and rejects submissions that exceed the permissible threshold, logging a `SECTION_TIMEOUT_FLAG` in `AssessmentAuditLogs`.

---

### Question 4: Automated Candidate Transfer Trigger Mechanics
**Examiner Question:**
> *"Describe the exact internal mechanics of the `trg_TransferAptiClearedCandidate` trigger. When does it fire, how does it distinguish between first-time completion vs re-evaluation, and why is `NOT EXISTS` evaluated?"*

**Model Answer:**
1. **Trigger Type & Event**: It is an `AFTER UPDATE` trigger defined on `dbo.TestAttempts`. It evaluates the pseudo-tables `inserted` and `deleted`.
2. **State Transition Guard**:
   `IF UPDATE(AttemptStatus) OR UPDATE(IsPassed)` ensures the trigger logic only executes when those specific columns change.
3. **Delta Evaluation**:
   ```sql
   WHERE i.AttemptStatus = 'Completed'
     AND i.IsPassed = 1
     AND (d.AttemptStatus <> 'Completed' OR d.IsPassed = 0)
   ```
   This condition verifies that the transition is a state change from non-completed/unpassed to passed, rather than an arbitrary update to an already completed exam.
4. **Idempotency via `NOT EXISTS`**:
   Before inserting into `AptiClearedCandidates`, it checks `NOT EXISTS (SELECT 1 FROM dbo.AptiClearedCandidates acc WHERE acc.CandidateId = i.CandidateId)`. This prevents primary/unique key constraint crashes if an attempt is re-saved, guaranteeing idempotent data promotion to the HR evaluation round.

---

### Question 5: Database Normalization (3NF) Justification
**Examiner Question:**
> *"Why did you normalize candidate academic qualifications into `CandidateEducations` and employment history into `CandidateExperiences` instead of storing them as JSON columns or comma-separated strings inside the `Candidates` table?"*

**Model Answer:**
1. **1NF Compliance (Atomicity)**:
   Storing comma-separated strings (e.g. `B.S. CS, M.S. Data Science`) violates First Normal Form because the attribute is not atomic and contains repeating groups.
2. **2NF & 3NF Compliance**:
   An applicant can possess multiple degrees and multiple prior employments (a $1:N$ relationship). Placing them in dedicated relations with their own primary keys (`EducationId`, `ExperienceId`) ensures non-key attributes (`InstituteName`, `PassingYear`, `PercentageOrCGPA`) depend strictly on the candidate's specific qualification instance.
3. **Relational Querying & Indexing**:
   Normalized tables allow the recruitment engine to run high-performance SQL index queries:
   ```sql
   SELECT CandidateId FROM dbo.CandidateEducations WHERE PercentageOrCGPA >= 80.00;
   ```
   If stored as JSON or delimited strings, the query engine would be forced to perform costly table scans with scalar string parsing or `OPENJSON` invocations.

---

### Question 6: Zero Answer Key Leakage in REST API Architecture
**Examiner Question:**
> *"How does your API prevent candidates from inspecting HTTP network response packets in DevTools to view the correct answers (`IsCorrect = true`) during an ongoing test?"*

**Model Answer:**
We maintain a strict separation between **Domain Entities** and **Client Data Transfer Objects (DTOs)**:
1. The domain entity `QuestionOption` contains `IsCorrect (bool)`. This entity is only utilized internally by the scoring service and EF Core mappings.
2. In `AssessmentService.GetSanitizedQuestionsForSectionAsync`, the query explicitly projects options into `CandidateQuestionOptionDto`:
   ```csharp
   new CandidateQuestionOptionDto(o.OptionId, o.OptionLabel, o.OptionText)
   ```
3. Because `CandidateQuestionOptionDto` does not have an `IsCorrect` property, the JSON serializer never emits this field into the HTTP response body. Even if an applicant inspects raw TCP/HTTP network frames, the correct answer boolean does not exist in the payload.

---

### Question 7: SQL Query Optimization: `NOT EXISTS` vs `LEFT JOIN / IS NULL`
**Examiner Question:**
> *"In Viva Query 1, why is `NOT EXISTS` preferred over `LEFT JOIN ... WHERE ... IS NULL` when isolating candidates who have not yet appeared for the test?"*

**Model Answer:**
- **Execution Plan Mechanics**:
  - `NOT EXISTS` allows the SQL Server query optimizer to generate a **Left Anti-Semi-Join** physical operator.
  - In an anti-semi-join, the engine searches the inner table (`TestAttempts`) using an index seek on `CandidateId`. The moment a single matching row is encountered for a candidate, evaluation for that candidate **halts immediately** and discards the record from the output stream.
- **`LEFT JOIN ... WHERE ... IS NULL` Drawbacks**:
  - Requires the engine to perform a full outer join, producing intermediate projected join rows for all attempts, spooling them into memory/tempdb, and then applying a post-filter predicate to discard non-null values.
  - `NOT EXISTS` provides superior execution plan efficiency, lower logical reads, and optimal memory grant allocation.

---

### Question 8: Window Functions: `DENSE_RANK()` vs `RANK()` in Recruitment Leaderboards
**Examiner Question:**
> *"In your Viva Query 2 leaderboard, why did you use `DENSE_RANK()` instead of `RANK()` or `ROW_NUMBER()`?"*

**Model Answer:**
- `ROW_NUMBER()` assigns sequential integers (1, 2, 3...) arbitrarily breaking ties, which is unfair when candidates achieve identical marks and percentages.
- `RANK()` assigns identical ranks to tied scores, but **skips subsequent rank numbers**. For example, if two candidates tie for Rank 1, the next candidate receives Rank 3 (1, 1, 3). In recruitment pipelines, this misrepresents merit tiers.
- `DENSE_RANK()` assigns identical ranks to tied scores without skipping sequence numbers (1, 1, 2, 3). This enables HR managers to define tier-based hiring cohorts (e.g. "Select all candidates in Merit Rank 1 and 2") without gaps in sequence numbering.

---

### Question 9: EF Core vs Dapper: Architectural Trade-Offs
**Examiner Question:**
> *"Your project architecture specifies both Entity Framework Core and Dapper. Where should each tool be used in an enterprise assessment system?"*

**Model Answer:**
- **Entity Framework Core (Unit of Work & Complex Domain Graphs)**:
  Best suited for transactional, domain-rich operations such as Candidate Enrollment (saving `Candidate` + `Educations` + `Experiences` in a single tracked graph), entity validations, and code-first migration workflows.
- **Dapper (High-Throughput Read & Bulk Scoring Execution)**:
  A lightweight micro-ORM that maps raw SQL results directly to memory without the change-tracker overhead of EF Core. Best suited for:
  - High-concurrency leaderboard generation (`vw_HrCandidatePipeline`).
  - Executing batch scoring procedures (`sp_SubmitSectionAnswers`) during simultaneous submissions from thousands of concurrent examinees.
  - Analytics and funnel aggregation reports (`sp_GetRecruitmentSummaryReport`).

---

### Question 10: Handling Network Latency in Online Submissions
**Examiner Question:**
> *"What happens if a candidate submits Section 1 with 1 second remaining, but network latency causes the packet to arrive at the server 4 seconds later?"*

**Model Answer:**
To prevent legitimate applicants from being unfairly penalized for external network jitter, the system implements a **configurable Network Grace Period** (15 seconds):
```csharp
var maxAllowedSeconds = (currentSection.TimeLimitMinutes * 60) + NetworkGraceSeconds;
```
- If the submission arrives within this 15-second grace window, it is processed and graded normally.
- If the submission arrives slightly after this grace window, it is accepted but flagged with a `SECTION_TIMEOUT_FLAG` audit record for administrative review.
- If the delay exceeds an egregious buffer (+120 seconds), the system permanently locks the attempt as `Timed_Out`.

---

### Question 11: Audit Logging Strategy for Forensic Analysis
**Examiner Question:**
> *"Why did you create a dedicated `AssessmentAuditLogs` table, and what specific events does it track?"*

**Model Answer:**
In university and enterprise recruitment, disputes may arise regarding test validity, technical interruptions, or disqualifications. `AssessmentAuditLogs` provides an immutable audit trail capturing:
1. `CANDIDATE_ENROLLED`: Timestamp and Manager who provisioned the candidate.
2. `ASSESSMENT_STARTED`: Exact UTC commencement of Round 1.
3. `SECTION_TRANSITION`: Sequential progression from GK to Math and Math to CS.
4. `SECTION_TIMEOUT_FLAG`: Latency overruns or timer expiry attempts.
5. `ASSESSMENT_FINALIZED`: Cumulative score, percentage, and pass/fail verdict.
6. `HR_PIPELINE_TRANSFER`: Automated promotion to `AptiClearedCandidates`.
7. `HR_STATUS_UPDATED`: Interview status changes and interviewer notes.

---

### Question 12: Centralized RFC 7807 Problem Details Error Handling
**Examiner Question:**
> *"Explain how your `GlobalExceptionMiddleware` adheres to the RFC 7807 standard and why this is important for enterprise APIs."*

**Model Answer:**
- **RFC 7807 ("Problem Details for HTTP APIs")** is the IETF industry standard specifying a uniform JSON machine-readable format for reporting API errors.
- Instead of returning generic HTML error pages or raw unhandled stack traces (which create security vulnerabilities by exposing database structure), `GlobalExceptionMiddleware`:
  1. Intercepts all unhandled exceptions globally.
  2. Maps domain-specific exceptions to standardized HTTP status codes:
     - `InvalidOperationException` $\to$ HTTP 400 Bad Request
     - `UnauthorizedAccessException` $\to$ HTTP 401 Unauthorized
     - `KeyNotFoundException` $\to$ HTTP 404 Not Found
     - `General Exception` $\to$ HTTP 500 Internal Server Error
  3. Returns a consistent schema containing `status`, `title`, `detail`, `instance`, and `type`.
  4. This enables front-end clients and mobile applications to display consistent, user-friendly error banners and reliably parse failure causes.
