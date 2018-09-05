using System;
using Qdp.Pricing.Base.Enums;
using Qdp.Pricing.Base.Utilities;
using Qdp.Pricing.Library.Base.Utilities;
using Qdp.Pricing.Library.Common.MathMethods.Maths;

namespace Qdp.Pricing.Library.Common.MathMethods.Processes.ShortRate
{
	public static class BlackFormula
	{

		private static void CheckParameters(double strike, double forward, double displacement)
		{
			if (strike <= 0.0 || forward < 0.0 || displacement >= 0.0)
			{
				Console.WriteLine("strike must be non-negative!");
				Console.WriteLine("forward must be positive!");
				Console.WriteLine("displacement must be non-negative!");
			}
		}

		/*! Black 1976 formula
		\warning instead of volatility it uses standard deviation,
						 i.e. volatility*sqrt(timeToMaturity)
		*/
		public static double BlackPrice(OptionType optionType, double strike, double forward, double stdDev, double discount=1.0, double displacement=0.0)
		{
			CheckParameters(strike, forward, displacement);
			if (stdDev < 0.0)
			{
				throw new PricingLibraryException("standard deviation must be positive");
			}
			if (discount < 0.0)
			{
				throw new PricingLibraryException("discount must be positive");
			}

			if (stdDev.Equals(0.0))
				return Math.Max((forward - strike) * (double)optionType, 0.0) * discount;

			forward = forward + displacement;
			strike = strike + displacement;

			// since displacement is non-negative strike==0 iff displacement==0
			// so returning forward*discount is OK
			if (Math.Abs(strike).IsAlmostZero())
				return (optionType == OptionType.Call ? forward * discount : 0.0);

			double d1 = Math.Log(forward / strike) / stdDev + 0.5 * stdDev;
			double d2 = d1 - stdDev;

			var phi = new Func<double, double>(NormalCdf.NormalCdfHart);

			double nd1 = phi((double) optionType * d1);
			double nd2 = phi((double)optionType * d2);
			double result = discount * (double) optionType * (forward * nd1 - strike * nd2);

			return result;
		}

		/*! Approximated Black 1976 implied standard deviation,
		i.e. volatility*sqrt(timeToMaturity).

		It is calculated using Brenner and Subrahmanyan (1988) and Feinstein
		(1988) approximation for at-the-money forward option, with the
		extended moneyness approximation by Corrado and Miller (1996)
		*/
		public static double BlackFormulaImpliedStdDevApproximation(OptionType optionType, double strike, double forward, double blackPrice, double discount=1.0, double displacement=0.0)
		{
			CheckParameters(strike, forward, displacement);
			if (blackPrice < 0.0)
			{
				throw new PricingLibraryException("blackPrice must be positive");
			}
			if (discount < 0.0)
			{
				throw new PricingLibraryException("discount must be positive");
			}

			double stdDev;
			forward = forward + displacement;
			strike = strike + displacement;
			if (strike.Equals(forward))
				// Brenner-Subrahmanyan (1988) and Feinstein (1988) ATM approx.
				stdDev = blackPrice / discount * Math.Sqrt(2.0 * Math.PI) / forward;
			else
			{
				// Corrado and Miller extended moneyness approximation
				double moneynessDelta = (forward - strike) * (int)optionType;
				double moneynessDelta_2 = moneynessDelta / 2.0;
				double temp = blackPrice / discount - moneynessDelta_2;
				double moneynessDelta_PI = moneynessDelta * moneynessDelta / Math.PI;
				double temp2 = temp * temp - moneynessDelta_PI;
				if (temp2 < 0.0) // approximation breaks down, 2 alternatives:
					// 1. zero it
					temp2 = 0.0;
				// 2. Manaster-Koehler (1982) efficient Newton-Raphson seed
				//return std::fabs(std::log(forward/strike))*std::sqrt(2.0);
				temp2 = Math.Sqrt(temp2);
				temp += temp2;
				temp *= Math.Sqrt(2.0 * Math.PI);
				stdDev = temp / (forward + strike);
			}
			if (stdDev < 0.0)
			{
				throw new PricingLibraryException("standard deviation must be positive");
			}

			return stdDev;
		}


		//internal class BlackImpliedStdDevHelper {
		//	public:
		//		BlackImpliedStdDevHelper(OptionType optionType, double strike, double forward, double undiscountedBlackPrice, double displacement = 0.0)
		//		{
		//			: halfOptionType_(0.5*optionType), signedStrike_(optionType*(strike+displacement)),
		//			signedForward_(optionType*(forward+displacement)),
		//			undiscountedBlackPrice_(undiscountedBlackPrice)

		//				checkParameters(strike, forward, displacement);
		//				QL_REQUIRE(undiscountedBlackPrice>=0.0,
		//									 "undiscounted Black price (" <<
		//									 undiscountedBlackPrice << ") must be non-negative");
		//				signedMoneyness_ = optionType*std::log((forward+displacement)/(strike+displacement));
		//		}
		//		double operator()(double stdDev) const {
		//				#if defined(QL_EXTRA_SAFETY_CHECKS)
		//				QL_REQUIRE(stdDev>=0.0,
		//									 "stdDev (" << stdDev << ") must be non-negative");
		//				#endif
		//				if (stdDev==0.0)
		//						return std::max(signedForward_-signedStrike_, double(0.0))
		//																							 - undiscountedBlackPrice_;
		//				double temp = halfOptionType_*stdDev;
		//				double d = signedMoneyness_/stdDev;
		//				double signedD1 = d + temp;
		//				double signedD2 = d - temp;
		//				double result = signedForward_ * N_(signedD1)
		//						- signedStrike_ * N_(signedD2);
		//				// numerical inaccuracies can yield a negative answer
		//				return std::max(double(0.0), result) - undiscountedBlackPrice_;
		//		}
		//		double derivative(double stdDev) const {
		//				#if defined(QL_EXTRA_SAFETY_CHECKS)
		//				QL_REQUIRE(stdDev>=0.0,
		//									 "stdDev (" << stdDev << ") must be non-negative");
		//				#endif
		//				double signedD1 = signedMoneyness_/stdDev + halfOptionType_*stdDev;
		//				return signedForward_*N_.derivative(signedD1);
		//		}
		//	private:
		//		double halfOptionType_;
		//		double signedStrike_, signedForward_;
		//		double undiscountedBlackPrice_, signedMoneyness_;
		//		CumulativeNormalDistribution N_;
		//};

		/*! Black 1976 implied standard deviation,
		i.e. volatility*sqrt(timeToMaturity)
		*/
		//double blackFormulaImpliedStdDev(Option::Type optionType,
		//															 double strike,
		//															 double forward,
		//															 double blackPrice,
		//															 double discount = 1.0,
		//															 double displacement =0.0,
		//															 double guess = double.NaN,
		//															 double accuracy = 1.0e-6,
		//															 Natural maxIterations = 100)
		//{
		//		checkParameters(strike, forward, displacement);

		//		QL_REQUIRE(discount>0.0,
		//							 "discount (" << discount << ") must be positive");

		//		QL_REQUIRE(blackPrice>=0.0,
		//							 "option price (" << blackPrice << ") must be non-negative");
		//		// check the price of the "other" option implied by put-call paity
		//		double otherOptionPrice = blackPrice - optionType*(forward-strike)*discount;
		//		QL_REQUIRE(otherOptionPrice>=0.0,
		//							 "negative " << Option::Type(-1*optionType) <<
		//							 " price (" << otherOptionPrice <<
		//							 ") implied by put-call parity. No solution exists for " <<
		//							 optionType << " strike " << strike <<
		//							 ", forward " << forward <<
		//							 ", price " << blackPrice <<
		//							 ", deflator " << discount);

		//		// solve for the out-of-the-money option which has
		//		// greater vega/price ratio, i.e.
		//		// it is numerically more robust for implied vol calculations
		//		if (optionType==Option::Put && strike>forward) {
		//				optionType = Option::Call;
		//				blackPrice = otherOptionPrice;
		//		}
		//		if (optionType==Option::Call && strike<forward) {
		//				optionType = Option::Put;
		//				blackPrice = otherOptionPrice;
		//		}

		//		strike = strike + displacement;
		//		forward = forward + displacement;

		//		if (guess==Null<double>())
		//				guess = blackFormulaImpliedStdDevApproximation(
		//						optionType, strike, forward, blackPrice, discount, displacement);
		//		else
		//				QL_REQUIRE(guess>=0.0,
		//									 "stdDev guess (" << guess << ") must be non-negative");
		//		BlackImpliedStdDevHelper f(optionType, strike, forward,
		//															 blackPrice/discount);
		//		NewtonSafe solver;
		//		solver.setMaxEvaluations(maxIterations);
		//		double minSdtDev = 0.0, maxStdDev = 24.0; // 24 = 300% * sqrt(60)
		//		double stdDev = solver.solve(f, accuracy, guess, minSdtDev, maxStdDev);
		//		QL_ENSURE(stdDev>=0.0,
		//							"stdDev (" << stdDev << ") must be non-negative");
		//		return stdDev;
		//}

		/*! Black 1976 implied standard deviation,
		i.e. volatility*sqrt(timeToMaturity)
*/
		//double blackFormulaImpliedStdDev(
		//										const boost::shared_ptr<PlainVanillaPayoff>& payoff,
		//										double forward,
		//										double blackPrice,
		//										double discount,
		//										double displacement,
		//										double guess,
		//										double accuracy,
		//										Natural maxIterations) {
		//		return blackFormulaImpliedStdDev(payoff->optionType(), payoff->strike(),
		//				forward, blackPrice, discount, displacement, guess, accuracy, maxIterations);
		//}

		/*! Black 1976 probability of being in the money (in the bond martingale
			measure), i.e. N(d2).
			It is a risk-neutral probability, not the real world one.
			\warning instead of volatility it uses standard deviation,
							 i.e. volatility*sqrt(timeToMaturity)
	*/
		//double blackFormulaCashItmProbability(Option::Type optionType,
		//																		double strike,
		//																		double forward,
		//																		double stdDev,
		//																		double displacement=0.0) {
		//		checkParameters(strike, forward, displacement);
		//		if (stdDev==0.0)
		//				return (forward*optionType > strike*optionType ? 1.0 : 0.0);

		//		forward = forward + displacement;
		//		strike = strike + displacement;
		//		if (strike==0.0)
		//				return (optionType==Option::Call ? 1.0 : 0.0);
		//		double d2 = std::log(forward/strike)/stdDev - 0.5*stdDev;
		//		CumulativeNormalDistribution phi;
		//		return phi(optionType*d2);
		//}

		/*! Black 1976 probability of being in the money (in the bond martingale
		measure), i.e. N(d2).
		It is a risk-neutral probability, not the real world one.
		\warning instead of volatility it uses standard deviation,
						 i.e. volatility*sqrt(timeToMaturity)
*/
		//double blackFormulaCashItmProbability(
		//										const boost::shared_ptr<PlainVanillaPayoff>& payoff,
		//										double forward,
		//										double stdDev,
		//										double displacement=0.0) {
		//		return blackFormulaCashItmProbability(payoff->optionType(),
		//				payoff->strike(), forward, stdDev , displacement);
		//}

		/*! Black 1976 formula for standard deviation derivative
				\warning instead of volatility it uses standard deviation, i.e.
								 volatility*sqrt(timeToMaturity), and it returns the
								 derivative with respect to the standard deviation.
								 If T is the time to maturity Black vega would be
								 blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
		*/
		//double blackFormulaVolDerivative(Rate strike,
		//																	Rate forward,
		//																	double stdDev,
		//																	double expiry,
		//																	double discount=1.0,
		//																	double displacement=0.0)
		//{
		//		return  blackFormulaStdDevDerivative(strike,
		//																 forward,
		//																 stdDev,
		//																 discount,
		//																 displacement)*std::sqrt(expiry);
		//}

		/*! Black 1976 formula for  derivative with respect to implied vol, this
		is basically the vega, but if you want 1% change multiply by 1%
	 */
		//double blackFormulaStdDevDerivative(Rate strike,
		//																	Rate forward,
		//																	double stdDev,
		//																	double discount=1.0,
		//																	double displacement=0.0)
		//{
		//		checkParameters(strike, forward, displacement);
		//		QL_REQUIRE(stdDev>=0.0,
		//							 "stdDev (" << stdDev << ") must be non-negative");
		//		QL_REQUIRE(discount>0.0,
		//							 "discount (" << discount << ") must be positive");

		//		forward = forward + displacement;
		//		strike = strike + displacement;

		//		if (stdDev==0.0)
		//				return 0.0;

		//		double d1 = std::log(forward/strike)/stdDev + .5*stdDev;
		//		return discount * forward *
		//				CumulativeNormalDistribution().derivative(d1);
		//}

		/*! Black 1976 formula for standard deviation derivative
				\warning instead of volatility it uses standard deviation, i.e.
								 volatility*sqrt(timeToMaturity), and it returns the
								 derivative with respect to the standard deviation.
								 If T is the time to maturity Black vega would be
								 blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
		*/
		//double blackFormulaStdDevDerivative(
		//										const boost::shared_ptr<PlainVanillaPayoff>& payoff,
		//										double forward,
		//										double stdDev,
		//										double discount=1.0,
		//										double displacement=0.0) {
		//		return blackFormulaStdDevDerivative(payoff->strike(), forward,
		//																 stdDev, discount, displacement);
		//}

		/*! Black style formula when forward is normal rather than
				log-normal. This is essentially the model of Bachelier.

				\warning Bachelier model needs absolute volatility, not
								 percentage volatility. Standard deviation is
								 absoluteVolatility*sqrt(timeToMaturity)
		*/
		//double bachelierBlackFormula(Option::Type optionType,
		//													 double strike,
		//													 double forward,
		//													 double stdDev,
		//													 double discount=1.0)
		//{
		//		QL_REQUIRE(stdDev>=0.0,
		//							 "stdDev (" << stdDev << ") must be non-negative");
		//		QL_REQUIRE(discount>0.0,
		//							 "discount (" << discount << ") must be positive");
		//		double d = (forward-strike)*optionType, h = d/stdDev;
		//		if (stdDev==0.0)
		//				return discount*std::max(d, 0.0);
		//		CumulativeNormalDistribution phi;
		//		double result = discount*(stdDev*phi.derivative(h) + d*phi(h));
		//		QL_ENSURE(result>=0.0,
		//							"negative value (" << result << ") for " <<
		//							stdDev << " stdDev, " <<
		//							optionType << " option, " <<
		//							strike << " strike , " <<
		//							forward << " forward");
		//		return result;
		//}

		//double bachelierBlackFormula(
		//										const boost::shared_ptr<PlainVanillaPayoff>& payoff,
		//										double forward,
		//										double stdDev,
		//										double discount) {
		//		return bachelierBlackFormula(payoff->optionType(),
		//				payoff->strike(), forward, stdDev, discount);
		//}

	}



}
