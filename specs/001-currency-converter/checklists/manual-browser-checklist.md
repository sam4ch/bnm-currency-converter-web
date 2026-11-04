# Interface validation — October 28, 2026

The assistant performed these scenarios in Opera GX 138.0.6044.45 through Playwright and inspected the screenshots visually. These were separate browser checks, not xUnit tests. No physical phone was used.

- [x] M001: At widths of 1280 and 390 px, the form was readable and had no horizontal overflow.
- [x] M002: Reviewed the Russian labels, input messages, successful result, and errors.
- [x] M003: Tab order was amount → source currency → swap → target currency → date → conversion; keyboard focus was accessible.
- [x] M004: Invalid input and future dates disabled the button; during a request, the button was disabled and the previous result was hidden.
- [x] M005: Cached data was offered without displaying a result; retrying without consent did not apply it. After confirmation, the calculation was correct; newer cached data was rejected.
- [x] M006: Same-currency conversion returned four decimal places without a rate link or date; a unit test additionally confirmed that neither the source nor the cache was called.
- [x] M007: Checked repeated requests, invalid → valid input, source unavailability with and without a cache, and recovery of the test source from 503 → 200 in the same form.

Details: [validation report](../../../docs/BROWSER_VALIDATION.md). User acceptance testing on a physical device remains a separate action for the student.
