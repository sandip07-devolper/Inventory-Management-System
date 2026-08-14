document.addEventListener("DOMContentLoaded", () => {
  if (AuthStorage.isAuthenticated()) {
    window.location.href = "dashboard.html";
    return;
  }

  wireLoginForm();
  wireRegisterForm();
});

function wireLoginForm() {
  const form = document.getElementById("loginForm");
  if (!form) return;

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    clearAlert();

    const email = document.getElementById("email").value.trim();
    const password = document.getElementById("password").value;

    try {
      setSubmitting(form, true);
      const result = await Api.login({ email, password });
      AuthStorage.setSession(result.token, result);
      window.location.href = "dashboard.html";
    } catch (err) {
      showAlert(err.message);
    } finally {
      setSubmitting(form, false);
    }
  });
}

function wireRegisterForm() {
  const form = document.getElementById("registerForm");
  if (!form) return;

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    clearAlert();

    const payload = {
      companyName: document.getElementById("companyName").value.trim(),
      adminFullName: document.getElementById("adminFullName").value.trim(),
      adminEmail: document.getElementById("adminEmail").value.trim(),
      password: document.getElementById("password").value
    };

    try {
      setSubmitting(form, true);
      const result = await Api.register(payload);
      AuthStorage.setSession(result.token, result);
      window.location.href = "dashboard.html";
    } catch (err) {
      showAlert(err.message);
    } finally {
      setSubmitting(form, false);
    }
  });
}

function showAlert(message) {
  const box = document.getElementById("alertBox");
  if (!box) return;
  box.textContent = message;
  box.classList.remove("d-none");
}

function clearAlert() {
  const box = document.getElementById("alertBox");
  if (box) box.classList.add("d-none");
}

function setSubmitting(form, isSubmitting) {
  const btn = form.querySelector("button[type=submit]");
  if (!btn) return;

  if (isSubmitting) {
    btn.dataset.originalText = btn.textContent;
    btn.textContent = "Please wait...";
    btn.disabled = true;
  } else {
    btn.textContent = btn.dataset.originalText || btn.textContent;
    btn.disabled = false;
  }
}
