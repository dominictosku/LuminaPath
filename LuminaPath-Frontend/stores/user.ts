import { LoginUser, CreateUser } from "~/utils/request";
import { User, type Credentials } from "~/utils/user";

export const useUseStore = defineStore("user", () => {
  const loggedIn: Ref<Boolean> = ref(false);
  const user: Ref<User> = ref(new User());
  const Token: Ref<String> = ref("");
  const getUser = computed(() => user.value);
  const config = computed(() => {
    return { headers: { Authorization: `Bearer ${Token.value}` } };
  });

  async function Create(Credentials: Credentials) {
    await CreateUser(Credentials);
    await Login(Credentials);
  }

  async function Login(Credentials: Credentials) {
    let token = await LoginUser(Credentials);
    if (typeof token === "string") {
      user.value.userName = Credentials.userName;
      Token.value = token;
      loggedIn.value = true;
    }
  }

  return { getUser, config, loggedIn, Login, Create };
});
